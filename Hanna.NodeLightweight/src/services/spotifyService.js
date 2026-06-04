const https = require('https');
const { config, boolEnv } = require('../core/config');
const { SecretFilterService } = require('./secretFilterService');

class SpotifyService {
  constructor(options = {}) {
    this.clientId = options.clientId ?? process.env.SPOTIFY_CLIENT_ID ?? '';
    this.clientSecret = options.clientSecret ?? process.env.SPOTIFY_CLIENT_SECRET ?? '';
    this.redirectUri = options.redirectUri ?? process.env.SPOTIFY_REDIRECT_URI ?? '';
    this.refreshToken = options.refreshToken ?? process.env.SPOTIFY_REFRESH_TOKEN ?? '';
    this.dryRun = options.dryRun ?? boolEnv('HANNA_SPOTIFY_DRY_RUN', config.dryRun);
    this.timeoutMs = options.timeoutMs || 15000;
    this.httpRequest = options.httpRequest || this.httpRequest.bind(this);
    this.filter = new SecretFilterService();
  }

  requiredConfig() {
    return {
      SPOTIFY_CLIENT_ID: this.clientId,
      SPOTIFY_CLIENT_SECRET: this.clientSecret,
      SPOTIFY_REDIRECT_URI: this.redirectUri,
      SPOTIFY_REFRESH_TOKEN: this.refreshToken
    };
  }

  missingConfig() {
    return Object.entries(this.requiredConfig()).filter(([, value]) => !value).map(([name]) => name);
  }

  isConfigured() { return this.missingConfig().length === 0; }

  status() {
    const missing = this.missingConfig();
    if (missing.length) {
      return {
        module: 'spotify',
        status: 'blocked_by_configuration',
        found: false,
        configured: false,
        dry_run: true,
        missing,
        message: 'Spotify OAuth no está configurado. Hanna puede ejecutar comandos en modo seguro sin guardar ni exponer secretos.'
      };
    }
    return {
      module: 'spotify',
      status: this.dryRun ? 'dry_run' : 'configured',
      found: true,
      configured: true,
      dry_run: this.dryRun,
      auth: 'refresh_token_configured',
      message: this.dryRun ? 'Spotify está configurado, pero las acciones de reproducción están en dry-run.' : 'Spotify está configurado para usar refresh token.'
    };
  }

  authStatus() {
    const state = this.status();
    return { ...state, auth_status: state.configured ? 'refresh_token_available' : 'missing_configuration' };
  }

  async getAccessToken() {
    if (!this.isConfigured()) return { status: 'missing_configuration', missing: this.missingConfig() };
    const body = new URLSearchParams({ grant_type: 'refresh_token', refresh_token: this.refreshToken }).toString();
    const auth = Buffer.from(`${this.clientId}:${this.clientSecret}`).toString('base64');
    const response = await this.httpRequest('https://accounts.spotify.com/api/token', {
      method: 'POST',
      headers: {
        Authorization: `Basic ${auth}`,
        'Content-Type': 'application/x-www-form-urlencoded',
        'Content-Length': Buffer.byteLength(body)
      },
      body
    });
    if (!response.ok || !response.json || !response.json.access_token) {
      return { status: 'service_unavailable', error: this.filter.redact(response.error || response.statusCode || 'spotify_auth_failed') };
    }
    return { status: 'ok', accessToken: response.json.access_token };
  }

  async search(query) {
    const cleanQuery = this.filter.redact(String(query || '').trim()).slice(0, 300);
    if (!cleanQuery) return { module: 'spotify', action: 'buscar', status: 'missing_configuration', message: 'Falta texto de búsqueda.' };
    if (!this.isConfigured()) return { ...this.status(), action: 'buscar', query: cleanQuery, dry_run: true };
    const token = await this.getAccessToken();
    if (token.status !== 'ok') return { module: 'spotify', action: 'buscar', ...token };
    const url = `https://api.spotify.com/v1/search?type=track&limit=5&q=${encodeURIComponent(cleanQuery)}`;
    const response = await this.httpRequest(url, { method: 'GET', headers: { Authorization: `Bearer ${token.accessToken}` } });
    if (!response.ok) return { module: 'spotify', action: 'buscar', status: 'service_unavailable', error: this.filter.redact(response.error || response.statusCode || 'spotify_search_failed') };
    const tracks = (((response.json || {}).tracks || {}).items || []).map(track => ({
      id: track.id,
      uri: track.uri,
      name: this.filter.redact(track.name || ''),
      artists: (track.artists || []).map(a => this.filter.redact(a.name || '')).filter(Boolean),
      album: this.filter.redact(track.album && track.album.name || '')
    }));
    return { module: 'spotify', action: 'buscar', status: 'ok', query: cleanQuery, tracks };
  }

  async play(query) {
    const cleanQuery = this.filter.redact(String(query || '').trim()).slice(0, 300);
    if (!cleanQuery) return { module: 'spotify', action: 'reproducir', status: 'missing_configuration', message: 'Falta texto para reproducir.' };
    if (!this.isConfigured()) return { ...this.status(), action: 'reproducir', query: cleanQuery, dry_run: true };
    if (this.dryRun) return { module: 'spotify', action: 'reproducir', status: 'dry_run', dry_run: true, query: cleanQuery, message: 'Reproducción omitida por HANNA_DRY_RUN/HANNA_SPOTIFY_DRY_RUN.' };
    const found = await this.search(cleanQuery);
    if (found.status !== 'ok' || !found.tracks.length) return { module: 'spotify', action: 'reproducir', status: found.status || 'not_found', query: cleanQuery, tracks: [] };
    const token = await this.getAccessToken();
    if (token.status !== 'ok') return { module: 'spotify', action: 'reproducir', ...token };
    const response = await this.httpRequest('https://api.spotify.com/v1/me/player/play', { method: 'PUT', headers: { Authorization: `Bearer ${token.accessToken}`, 'Content-Type': 'application/json' }, body: JSON.stringify({ uris: [found.tracks[0].uri] }) });
    if (!response.ok) return { module: 'spotify', action: 'reproducir', status: 'service_unavailable', error: this.filter.redact(response.error || response.statusCode || 'spotify_play_failed') };
    return { module: 'spotify', action: 'reproducir', status: 'ok', track: found.tracks[0] };
  }

  async pause() { return this.playerAction('pausar', 'PUT', '/v1/me/player/pause'); }
  async next() { return this.playerAction('siguiente', 'POST', '/v1/me/player/next'); }
  async previous() { return this.playerAction('anterior', 'POST', '/v1/me/player/previous'); }

  async playerAction(action, method, path) {
    if (!this.isConfigured()) return { ...this.status(), action, dry_run: true };
    if (this.dryRun) return { module: 'spotify', action, status: 'dry_run', dry_run: true, message: `Acción ${action} omitida por HANNA_DRY_RUN/HANNA_SPOTIFY_DRY_RUN.` };
    const token = await this.getAccessToken();
    if (token.status !== 'ok') return { module: 'spotify', action, ...token };
    const response = await this.httpRequest(`https://api.spotify.com${path}`, { method, headers: { Authorization: `Bearer ${token.accessToken}` } });
    if (!response.ok) return { module: 'spotify', action, status: 'service_unavailable', error: this.filter.redact(response.error || response.statusCode || `spotify_${action}_failed`) };
    return { module: 'spotify', action, status: 'ok' };
  }

  httpRequest(urlString, options = {}) {
    return new Promise(resolve => {
      const url = new URL(urlString);
      const body = options.body || '';
      const req = https.request({
        method: options.method || 'GET',
        hostname: url.hostname,
        port: url.port || undefined,
        path: url.pathname + url.search,
        timeout: this.timeoutMs,
        headers: options.headers || {}
      }, res => {
        let data = '';
        res.on('data', chunk => { data += chunk; });
        res.on('end', () => {
          let json = null;
          try { json = data ? JSON.parse(data) : {}; } catch { json = null; }
          resolve({ ok: res.statusCode >= 200 && res.statusCode < 300, statusCode: res.statusCode, json, body: this.filter.redact(data).slice(0, 500) });
        });
      });
      req.on('timeout', () => { req.destroy(); resolve({ ok: false, status: 'service_unavailable', error: 'timeout' }); });
      req.on('error', error => resolve({ ok: false, status: 'service_unavailable', error: this.filter.redact(error.message) }));
      if (body) req.write(body);
      req.end();
    });
  }
}

module.exports = { SpotifyService };

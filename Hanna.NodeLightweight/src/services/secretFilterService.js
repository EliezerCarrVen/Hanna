class SecretFilterService {
  redact(input) {
    let text = String(input ?? '');
    const names = '(api[_-]?key|token|bearer|password|pwd|secret|client[_-]?secret|refresh[_-]?token|TELEGRAM_TOKEN|GROQ_API_KEY|GEMINI_API_KEY|OPENROUTER_API_KEY|SPOTIFY_CLIENT_SECRET|MYSQL_PASSWORD|HANNA_JWT_SECRET)';
    text = text.replace(new RegExp(`(${names}\s*[:=]\s*)[^\s,;]+`, 'gi'), '$1[REDACTED]');
    text = text.replace(/https?:\/\/[^\s:/?#]+:[^\s@/]+@/gi, 'https://[REDACTED]@');
    text = text.replace(/\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b/g, '[REDACTED]');
    text = text.replace(/\b(?:sk-|ghp_|xox[baprs]-)[A-Za-z0-9_\-]{16,}\b/g, '[REDACTED]');
    text = text.replace(/\b[A-Za-z0-9_\-]{40,}\b/g, '[REDACTED]');
    return text;
  }
}
module.exports = { SecretFilterService };

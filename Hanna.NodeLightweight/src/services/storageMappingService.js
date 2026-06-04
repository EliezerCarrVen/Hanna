const fs = require('fs');
const path = require('path');
const { paths } = require('../core/paths');
const { ensureDir, ensureFile } = require('../utils/fsSafe');
const { JsonlStoreService } = require('./jsonlStoreService');
const { MarkdownVaultService } = require('./markdownVaultService');
const { SecretFilterService } = require('./secretFilterService');

class StorageMappingService {
  constructor(options = {}) {
    this.markdown = options.markdown || new MarkdownVaultService();
    this.filter = new SecretFilterService();
    this.configFile = options.configFile || paths.systemConfig;
    this.markdownCollections = {
      memorias: 'memoria',
      contexto_proyectos: 'proyectos',
      codigo_generado: 'codigo_cache'
    };
    this.jsonlCollections = {
      conversaciones: path.join(paths.runtime, 'conversaciones.jsonl'),
      mensajes: path.join(paths.runtime, 'mensajes.jsonl'),
      transcripciones_audio: path.join(paths.runtime, 'transcripciones_audio.jsonl'),
      analisis_pantalla: path.join(paths.runtime, 'analisis_pantalla.jsonl'),
      acciones_agente: path.join(paths.runtime, 'acciones_agente.jsonl')
    };
  }

  status() {
    return {
      status: 'ok',
      mongodb: 'not_required_i386',
      markdown_collections: Object.keys(this.markdownCollections),
      jsonl_collections: Object.keys(this.jsonlCollections),
      estado_sistema: this.configFile
    };
  }

  collectionType(collection) {
    if (this.markdownCollections[collection]) return 'markdown';
    if (this.jsonlCollections[collection]) return 'jsonl';
    if (collection === 'estado_sistema') return 'config_json';
    return 'unsupported_collection';
  }

  write(collection, payload = {}) {
    const type = this.collectionType(collection);
    if (type === 'markdown') return this.writeMarkdown(collection, payload);
    if (type === 'jsonl') return this.writeJsonl(collection, payload);
    if (type === 'config_json') return this.writeSystemState(payload);
    return { status: 'unsupported_collection', collection };
  }

  read(collection, queryOrLimit = '') {
    const type = this.collectionType(collection);
    if (type === 'markdown') return this.searchMarkdown(collection, String(queryOrLimit || ''));
    if (type === 'jsonl') return { status: 'ok', collection, type, items: new JsonlStoreService(this.jsonlCollections[collection]).read(Number(queryOrLimit) || 50) };
    if (type === 'config_json') return this.readSystemState();
    return { status: 'unsupported_collection', collection };
  }

  writeMarkdown(collection, payload) {
    const area = this.markdownCollections[collection];
    const title = this.filter.redact(String(payload.title || payload.id || collection).slice(0, 120));
    const body = typeof payload === 'string' ? payload : JSON.stringify(payload, null, 2);
    const result = this.markdown.createNote(area, title, this.filter.redact(body));
    return { status: 'ok', collection, type: 'markdown', area, file: result.file };
  }

  searchMarkdown(collection, query) {
    const area = this.markdownCollections[collection];
    const hits = this.markdown.search(query).filter(item => String(item.path || '').includes(`${path.sep}${area}${path.sep}`));
    return { status: 'ok', collection, type: 'markdown', area, items: hits };
  }

  writeJsonl(collection, payload) {
    const file = this.jsonlCollections[collection];
    const store = new JsonlStoreService(file);
    const entry = { timestamp: new Date().toISOString(), collection, data: this.sanitize(payload) };
    store.append(entry);
    return { status: 'ok', collection, type: 'jsonl', file, entry };
  }

  readSystemState() {
    ensureFile(this.configFile, '{\n  "estado_sistema": {}\n}\n');
    try { return { status: 'ok', collection: 'estado_sistema', type: 'config_json', data: JSON.parse(fs.readFileSync(this.configFile, 'utf8')) }; }
    catch { return { status: 'parse_error', collection: 'estado_sistema', type: 'config_json', data: {} }; }
  }

  writeSystemState(payload) {
    ensureDir(path.dirname(this.configFile));
    const current = this.readSystemState().data || {};
    const next = { ...current, estado_sistema: { ...(current.estado_sistema || {}), ...this.sanitize(payload), updated_at: new Date().toISOString() } };
    fs.writeFileSync(this.configFile, JSON.stringify(next, null, 2) + '\n');
    return { status: 'ok', collection: 'estado_sistema', type: 'config_json', file: this.configFile };
  }

  sanitize(value) {
    if (typeof value === 'string') return this.filter.redact(value);
    return JSON.parse(JSON.stringify(value || {}, (key, val) => /token|secret|password|api[_-]?key/i.test(key) ? '[REDACTED]' : val));
  }
}

module.exports = { StorageMappingService };

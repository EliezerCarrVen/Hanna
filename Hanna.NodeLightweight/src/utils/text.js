function truncateText(value, maxBytes = 4096) {
  const text = String(value ?? '');
  const buffer = Buffer.from(text, 'utf8');
  if (buffer.length <= maxBytes) return text;
  return buffer.subarray(0, maxBytes).toString('utf8') + '…[truncated]';
}
function slugify(value) {
  return String(value || 'nota').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().replace(/[^a-z0-9_-]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 80) || 'nota';
}
function nowIso() { return new Date().toISOString(); }
module.exports = { truncateText, slugify, nowIso };

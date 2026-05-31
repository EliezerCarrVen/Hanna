const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const cp = require('child_process');

const base = path.resolve(process.argv[2] || process.cwd());
const memoryDir = path.join(base, 'memoria_jerarquica');
const indexDir = path.join(memoryDir, 'index');
const dailyDir = path.join(memoryDir, 'daily');
const archiveDir = path.join(memoryDir, 'archive');
const auditDir = path.join(base, 'auditoria');
const indexJsonl = path.join(indexDir, 'index.jsonl');
const indexDb = path.join(indexDir, 'index.db');
for (const d of [memoryDir,indexDir,dailyDir,archiveDir,auditDir]) fs.mkdirSync(d,{recursive:true});

function today(){ return new Date().toISOString().slice(0,10); }
function sha256(p){ const h=crypto.createHash('sha256'); h.update(fs.readFileSync(p)); return h.digest('hex'); }
function readTextSafe(p, limit=120000){ try { const s=fs.readFileSync(p,'utf8'); return s.length>limit?s.slice(-limit):s; } catch { return ''; } }
function listFiles(dir, arr=[]){ if(!fs.existsSync(dir)) return arr; for(const it of fs.readdirSync(dir,{withFileTypes:true})){ const p=path.join(dir,it.name); if(it.isDirectory()) listFiles(p,arr); else arr.push(p); } return arr; }
function summarize(text){
  const lines = text.split(/\r?\n/).map(x=>x.trim()).filter(Boolean);
  const important = lines.filter(l=>/error|fall|token|motor|spotify|api|mongo|mysql|ollama|openrouter|gemini|groq|hanna|proyecto|codigo|código|backup|memoria|fase|netflix|tv lg/i.test(l));
  const chosen = (important.length?important:lines).slice(-80);
  return chosen.join('\n').slice(-6000) || 'Sin actividad relevante detectada.';
}
function writeJsonl(p,obj){ fs.appendFileSync(p, JSON.stringify(obj)+'\n','utf8'); }
function sqlEscape(s){ return String(s||'').replace(/'/g,"''"); }
function ensureSqlite(){
  try {
    const res=cp.spawnSync('sqlite3',['-version'],{encoding:'utf8'});
    if(res.status!==0) return false;
    const schema=`CREATE TABLE IF NOT EXISTS memory_index(id INTEGER PRIMARY KEY AUTOINCREMENT,date TEXT NOT NULL,type TEXT NOT NULL DEFAULT 'daily_summary',summary TEXT NOT NULL,tags TEXT DEFAULT '',location TEXT NOT NULL DEFAULT 'LOCAL',archive_path TEXT DEFAULT '',hash TEXT DEFAULT '',created_at TEXT NOT NULL,updated_at TEXT NOT NULL);CREATE INDEX IF NOT EXISTS idx_memory_index_date ON memory_index(date);CREATE INDEX IF NOT EXISTS idx_memory_index_location ON memory_index(location);CREATE INDEX IF NOT EXISTS idx_memory_index_hash ON memory_index(hash);`;
    cp.spawnSync('sqlite3',[indexDb,schema],{encoding:'utf8'});
    return true;
  } catch { return false; }
}
function insertSqlite(entry){
  if(!ensureSqlite()) return false;
  const now=new Date().toISOString();
  const sql=`INSERT INTO memory_index(date,type,summary,tags,location,archive_path,hash,created_at,updated_at) VALUES('${sqlEscape(entry.date)}','${sqlEscape(entry.type||'daily_summary')}','${sqlEscape(entry.summary)}','${sqlEscape(Array.isArray(entry.tags)?entry.tags.join(','):entry.tags)}','${sqlEscape(entry.location||'LOCAL')}','${sqlEscape(entry.file||entry.archive||'')}','${sqlEscape(entry.hash||'')}','${now}','${now}');`;
  const r=cp.spawnSync('sqlite3',[indexDb,sql],{encoding:'utf8'});
  return r.status===0;
}

const date=today();
const sources=[path.join(base,'registros_conversacion'), path.join(base,'contexto_chats'), path.join(base,'contexto_persistente'), path.join(base,'logs')];
let collected='';
for(const s of sources){
  for(const f of listFiles(s)){
    const st=fs.statSync(f);
    if(Date.now()-st.mtimeMs < 36*3600*1000 && st.size < 5_000_000) collected += `\n\n--- ${path.relative(base,f)} ---\n` + readTextSafe(f);
  }
}
const summary=summarize(collected);
const dayFile=path.join(dailyDir, `${date}.summary.md`);
fs.writeFileSync(dayFile, `# Resumen diario Hanna ${date}\n\n${summary}\n`, 'utf8');
const hash=sha256(dayFile);
const entry={date, type:'daily_summary', summary: summary.slice(0,1200), tags:['hanna','daily','offline'], location:'LOCAL', file:path.relative(base,dayFile), hash, createdAt:new Date().toISOString()};
writeJsonl(indexJsonl, entry);
insertSqlite(entry);
writeJsonl(path.join(auditDir, `audit.jsonl`), {at:new Date().toISOString(), action:'daily_consolidation', file:path.relative(base,dayFile), hash, ok:true});

try {
  const zstd=cp.spawnSync('zstd',['--version'],{encoding:'utf8'});
  if(zstd.status===0){
    const out=path.join(archiveDir, `${date}.summary.md.zst`);
    cp.spawnSync('zstd',['-q','-f',dayFile,'-o',out],{stdio:'ignore'});
    if(fs.existsSync(out)) {
      const arc={date,type:'compressed_daily_summary',summary:summary.slice(0,1200),tags:['hanna','daily','compressed'],location:'LOCAL_ARCHIVE',file:path.relative(base,out),hash:sha256(out),createdAt:new Date().toISOString()};
      writeJsonl(indexJsonl,arc);
      insertSqlite(arc);
    }
  }
} catch {}
console.log(`Hanna V6.2 mantenimiento OK: ${dayFile}`);
console.log(`Índice JSONL: ${indexJsonl}`);
console.log(`Índice SQLite: ${indexDb}`);

import type { PendingFile } from '../types/hanna';

interface Props {
  files: PendingFile[];
  onAdd: (files: File[]) => void;
  onRemove: (id: string) => void;
}

function formatSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function FileDropzone({ files, onAdd, onRemove }: Props) {
  function handleFiles(fileList: FileList | null) {
    if (!fileList) return;
    onAdd(Array.from(fileList));
  }

  return (
    <section
      className="file-dropzone"
      onDragOver={(event) => event.preventDefault()}
      onDrop={(event) => {
        event.preventDefault();
        handleFiles(event.dataTransfer.files);
      }}
    >
      <label className="attach-button">
        Adjuntar
        <input type="file" multiple onChange={(event) => handleFiles(event.target.files)} />
      </label>
      <span className="drop-hint">Arrastra archivos aquí. Subida preparada, no automática.</span>
      {files.length > 0 && (
        <div className="file-list">
          {files.map((item) => (
            <div className="file-pill" key={item.id}>
              <span>{item.file.name}</span>
              <small>{formatSize(item.file.size)} · {item.file.type || 'tipo no definido'} · {item.status}</small>
              <button type="button" onClick={() => onRemove(item.id)} aria-label={`Quitar ${item.file.name}`}>×</button>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

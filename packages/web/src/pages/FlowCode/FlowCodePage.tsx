import { useState } from 'react';
import { useBridge } from '../../bridge/useBridge';
import { useFlowStore } from '../../store/flowStore';

export function FlowCodePage(): JSX.Element {
  const bridge = useBridge();
  const profiles = useFlowStore((s: import('../../store/flowStore').FlowState) => s.profiles);
  const [exportProfileId, setExportProfileId] = useState<string>(profiles[0]?.id ?? 'default');
  const [encrypt, setEncrypt] = useState<boolean>(false);
  const [exportedCode, setExportedCode] = useState<string>('');
  const [importCode, setImportCode] = useState<string>('');
  const [importPassphrase, setImportPassphrase] = useState<string>('');
  const [status, setStatus] = useState<{ kind: 'idle' | 'success' | 'error'; message: string }>({ kind: 'idle', message: '' });

  return (
    <section style={{ padding: '16px', maxWidth: '720px', display: 'grid', gap: '24px' }}>
      <h2>Flow Code</h2>

      <fieldset style={{ border: '1px solid var(--fr-border, #dadbe1)', borderRadius: '8px', padding: '12px' }}>
        <legend>导出</legend>
        <label>
          Profile：
          <select value={exportProfileId} onChange={(e) => setExportProfileId(e.target.value)} style={{ marginLeft: '8px' }}>
            {profiles.map((p: import('../../store/flowStore').ProfileSummary) => (
              <option key={p.id} value={p.id}>{p.name}</option>
            ))}
            {profiles.length === 0 && <option value="default">默认</option>}
          </select>
        </label>
        <label style={{ marginLeft: '12px' }}>
          <input
            type="checkbox"
            checked={encrypt}
            onChange={(e) => setEncrypt(e.target.checked)}
          />
          启用 AES-256-GCM 加密
        </label>
        <div style={{ marginTop: '12px' }}>
          <button
            type="button"
            onClick={() => {
              void bridge.exportFlowCode({ profileId: exportProfileId, encrypt }).then((code: string) => {
                setExportedCode(code);
                setStatus({ kind: 'success', message: 'Flow Code 已生成（占位实现，Phase 7 接通真实编码）' });
              }).catch((e: unknown) => {
                setStatus({ kind: 'error', message: e instanceof Error ? e.message : String(e) });
              });
            }}
          >
            生成 Flow Code
          </button>
        </div>
        {exportedCode !== '' && (
          <div style={{ marginTop: '12px' }}>
            <textarea
              readOnly
              value={exportedCode}
              style={{ width: '100%', minHeight: '120px', fontFamily: 'monospace' }}
            />
            <button
              type="button"
              onClick={() => {
                void navigator.clipboard.writeText(exportedCode);
              }}
              style={{ marginTop: '8px' }}
            >
              复制
            </button>
          </div>
        )}
      </fieldset>

      <fieldset style={{ border: '1px solid var(--fr-border, #dadbe1)', borderRadius: '8px', padding: '12px' }}>
        <legend>导入</legend>
        <textarea
          placeholder="粘贴 Flow Code"
          value={importCode}
          onChange={(e) => setImportCode(e.target.value)}
          style={{ width: '100%', minHeight: '120px', fontFamily: 'monospace' }}
        />
        <input
          type="password"
          placeholder="口令（如果加密）"
          value={importPassphrase}
          onChange={(e) => setImportPassphrase(e.target.value)}
          style={{ width: '100%', marginTop: '8px' }}
        />
        <div style={{ marginTop: '12px' }}>
          <button
            type="button"
            disabled={importCode.trim().length === 0}
            onClick={() => {
              void bridge.importFlowCode({ code: importCode, passphrase: importPassphrase.length === 0 ? null : importPassphrase }).then((r: import('../../bridge/useBridge').ImportResult) => {
                if (r.ok) {
                  setStatus({ kind: 'success', message: '导入成功（占位实现）' });
                } else {
                  setStatus({ kind: 'error', message: r.error ?? '未知错误' });
                }
              });
            }}
          >
            预览并应用
          </button>
        </div>
      </fieldset>

      {status.kind !== 'idle' && (
        <p style={{ color: status.kind === 'success' ? 'green' : 'crimson' }}>{status.message}</p>
      )}
    </section>
  );
}
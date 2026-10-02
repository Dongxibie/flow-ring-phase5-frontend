import { useFlowStore } from '../../store/flowStore';

const TRIGGER_KEY_OPTIONS = [
  { value: 'MouseSideButton', label: '鼠标侧键长按' },
  { value: 'MiddleButton', label: '中键长按' },
  { value: 'RightButtonLongPress', label: '右键长按' },
  { value: 'HotKey', label: '自定义热键' },
];

export function SettingsPage(): JSX.Element {
  const settings = useFlowStore((s: import('../../store/flowStore').FlowState) => s.settings);
  const update = useFlowStore((s: import('../../store/flowStore').FlowState) => s.updateSettings);

  return (
    <section style={{ padding: '16px', maxWidth: '720px' }}>
      <h2>设置</h2>

      <fieldset style={{ border: '1px solid var(--fr-border, #dadbe1)', borderRadius: '8px', padding: '12px', marginBottom: '12px' }}>
        <legend>触发键</legend>
        <label>
          默认触发：
          <select
            value={settings.triggerKey}
            onChange={(e) => update({ triggerKey: e.target.value })}
            style={{ marginLeft: '8px' }}
          >
            {TRIGGER_KEY_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </label>
        <p style={{ fontSize: '12px', opacity: 0.7, marginTop: '4px' }}>
          MVP 默认仅 MouseSideButton，其他项在 v1.1 启用。
        </p>
      </fieldset>

      <fieldset style={{ border: '1px solid var(--fr-border, #dadbe1)', borderRadius: '8px', padding: '12px', marginBottom: '12px' }}>
        <legend>Dead Zone</legend>
        <label>
          半径（像素）：
          <input
            type="number"
            min={10}
            max={120}
            value={settings.deadZoneRadiusPx}
            onChange={(e) => update({ deadZoneRadiusPx: Math.max(10, Math.min(120, Number(e.target.value))) })}
            style={{ marginLeft: '8px' }}
          />
        </label>
      </fieldset>

      <fieldset style={{ border: '1px solid var(--fr-border, #dadbe1)', borderRadius: '8px', padding: '12px', marginBottom: '12px' }}>
        <legend>动画时长</legend>
        <label>
          持续（毫秒）：
          <input
            type="number"
            min={0}
            max={1000}
            value={settings.animationDurationMs}
            onChange={(e) => update({ animationDurationMs: Math.max(0, Math.min(1000, Number(e.target.value))) })}
            style={{ marginLeft: '8px' }}
          />
        </label>
        <p style={{ fontSize: '12px', opacity: 0.7, marginTop: '4px' }}>
          MVP 不接自定义主题和动画时长，本字段仅写值不生效（v1.1 启用）。
        </p>
      </fieldset>

      <fieldset style={{ border: '1px solid var(--fr-border, #dadbe1)', borderRadius: '8px', padding: '12px' }}>
        <legend>主题</legend>
        <label>
          <input
            type="radio"
            name="theme"
            checked={settings.theme === 'auto'}
            onChange={() => update({ theme: 'auto' })}
          />
          跟随系统
        </label>
        <label style={{ marginLeft: '12px' }}>
          <input
            type="radio"
            name="theme"
            checked={settings.theme === 'light'}
            onChange={() => update({ theme: 'light' })}
          />
          浅色
        </label>
        <label style={{ marginLeft: '12px' }}>
          <input
            type="radio"
            name="theme"
            checked={settings.theme === 'dark'}
            onChange={() => update({ theme: 'dark' })}
          />
          深色
        </label>
      </fieldset>

      <div style={{ marginTop: '16px' }}>
        <button type="button">保存设置</button>
      </div>
    </section>
  );
}
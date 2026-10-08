> **本仓库是 [Flow Ring](https://github.com/Dongxibie/flow-ring-phase7-package) 的分阶段开发记录（第 5 步 · 前端四页）。**
> 完整产品（源码 / 截图 / 下载即用的 Windows 安装包）在 **[flow-ring-phase7-package](https://github.com/Dongxibie/flow-ring-phase7-package)**，建议从产品仓库开始了解本项目。

---

# Flow Ring · 第 5 步：前端四页

Flow Ring 是一个 Windows 桌面快捷环：按住鼠标侧键唤出悬浮圆环，把光标拖向某个方向后松开，即可执行常用动作，不用离开当前窗口。项目按开发阶段拆分为 7 个仓库，本仓库是其中的第 5 步。

## 本步骤完成内容

前端四个页面与配套状态层：

- `useBridge`（WebView2 + stub）
- `flowStore`（zustand）
- ProfileManagerPage：列表卡片
- RingStudioPage：三栏拖拽（Action 库 + 实时预览 + 属性面板）
- SettingsPage：触发键 + dead zone + 主题
- FlowCodePage：导出 / 导入

## 验证结果（阶段记录）

- `pnpm build` 通过，产物 16.72 kB + 208.59 kB（gzip 68.15 kB）

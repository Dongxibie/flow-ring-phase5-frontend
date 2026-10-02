import { create } from 'zustand';

export interface ProfileSummary {
  id: string;
  name: string;
  isDefault: boolean;
}

export interface SettingsDraft {
  triggerKey: string;
  deadZoneRadiusPx: number;
  animationDurationMs: number;
  theme: 'light' | 'dark' | 'auto';
}

export interface FlowState {
  isPaused: boolean;
  activeProfileId: string;
  profiles: ProfileSummary[];
  settings: SettingsDraft;

  setPaused: (paused: boolean) => void;
  setActiveProfile: (id: string) => void;
  setProfiles: (profiles: ProfileSummary[]) => void;
  updateSettings: (patch: Partial<SettingsDraft>) => void;
}

const defaultSettings: SettingsDraft = {
  triggerKey: 'MouseSideButton',
  deadZoneRadiusPx: 30,
  animationDurationMs: 180,
  theme: 'auto',
};

export const useFlowStore = create<FlowState>((set) => ({
  isPaused: false,
  activeProfileId: 'default',
  profiles: [],
  settings: defaultSettings,

  setPaused: (paused) => set({ isPaused: paused }),
  setActiveProfile: (id) => set({ activeProfileId: id }),
  setProfiles: (profiles) => set({ profiles }),
  updateSettings: (patch) =>
    set((state) => ({ settings: { ...state.settings, ...patch } })),
}));
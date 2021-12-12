export interface TabStatsHeader {
	downloads: string;
	runningInstances: string;
	runningBots: string;
	playbackTime: string;
}

export interface ProjectInfo {
	project: string;
	projectName: string;
	commitUrl: string;
	extended: boolean;
	notification?: string;
	builds: BuildInfo[];
}

export interface BuildInfo {
	active?: boolean;
	branch: string;
	commit: string;
	version: string;
	zipContent: boolean;
	fileName: string;
	uploadTime: string;
	downloadCount?: number;
}

export interface LangInfo {
	project: string;
	language: string;
	uploadTime: string;
	displayName: string;
	downloadCount?: number;
}

export interface KeyValue {
	key: string;
	value: string;
}

export interface LoginResult {
	loggedIn: boolean;
	user: {
		id: number;
		name: string;
		rank: number;
	}
}

export interface BlogViewData {
	title: string;
	summaryHtml: string;
	contentHtml: string;
	tags: string[];
}

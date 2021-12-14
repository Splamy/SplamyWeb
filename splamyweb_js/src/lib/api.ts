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

export interface BlogPostShortView {
	postId: number;
	createTime: string;
	title: string;
	summaryHtml: string;
	tags: string[];
}

export interface BlogViewData extends BlogPostShortView {
	contentHtml: string;
	recentPosts: BlogPostShortView[];
}

export interface BlogPostUpdate {
	postId?: number;
	visible?: boolean;
	contentRaw?: string;
	tags?: string[];
}

export interface BlogListQuery {
	pages: number;
	posts: BlogPostShortView[];
}

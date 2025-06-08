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
	visible?: boolean;
	createTime: string;
	title: string;
	summaryHtml: string;
	tags: string[];
}

export const EMPTY_POST = () => { return {
	postId: 0,
	createTime: '',
	title: 'Not Found',
	summaryHtml: '',
	contentHtml: '',
	tags: []
} as BlogPostView; };

export interface BlogPostView extends BlogPostShortView {
	contentHtml: string;
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
	recentPosts?: BlogPostShortView[]; // not returned for now
}

export interface BlogItemQuery {
	post: BlogPostView;
	recentPosts?: BlogPostShortView[];
}

export interface RamsesSystemStats {
	indexedSongs: string;
	indexedDifficulties: string;
	totalSize: string;
}

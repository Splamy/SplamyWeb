export async function loadMinigame() {
	return await import('./MiniGame.svelte').then(module => module.default);
}

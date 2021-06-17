import "svelte";
import UiApp from "../Svelte/SvPage.svelte";

export function init() {
	const logtable = document.getElementById("log_table");
	return new UiApp({
		target: logtable,
	});
}

import adapter from '@sveltejs/adapter-static';
import preprocess from 'svelte-preprocess';
import { defineConfig } from "vite";


/** @type {import('@sveltejs/kit').Config} */
const config = {
	// Consult https://github.com/sveltejs/svelte-preprocess
	// for more information about preprocessors
	preprocess: preprocess(),

	kit: {
		adapter: adapter(),

		// hydrate the <div id="svelte"> element in src/app.html
		target: '#svelte',
		router: true,
		prerender: {
			onError: "continue"
		}
	},
};

export default config;

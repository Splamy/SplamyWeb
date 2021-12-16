import adapter from '@sveltejs/adapter-static';
import preprocess from 'svelte-preprocess';


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
			crawl: true,
			enabled: true,
			onError: "continue",
		},

		vite: {
			build: {
				rollupOptions: {
					output: {
						// https://github.com/sveltejs/kit/issues/1632
						// https://github.com/sveltejs/kit/issues/1571
						// https://github.com/vitejs/vite/issues/3731
						manualChunks: undefined
					}
				}
			}
		}
	},
};

export default config;

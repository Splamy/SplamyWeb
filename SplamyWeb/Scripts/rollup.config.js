import resolve from '@rollup/plugin-node-resolve';
import { defineConfig } from 'rollup';
import svelte from 'rollup-plugin-svelte';
import typescript from 'rollup-plugin-typescript';
import commonjs from '@rollup/plugin-commonjs';
import preprocess from 'svelte-preprocess';

function buildPage(name) {
	return defineConfig({
		input: `Pages/${name}.ts`,
		output: {
			file: `../wwwroot/js/${name}.js`,
			format: "iife",
			sourcemap: false,
			name,
		},
		plugins: [
			svelte({
				preprocess: preprocess(),
				emitCss: false,
				onwarn: (warning, handler) => {
					if (warning.code === 'a11y-distracting-elements') return;
					handler(warning);
				},

				// You can pass any of the Svelte compiler options
				compilerOptions: {
					//generate: 'ssr',
					//hydratable: true,
					//customElement: true
				}
			}),
			resolve({ browser: true }),
			typescript({ tsconfig: './tsconfig.json' }),
			commonjs({ extensions: ['.js', '.ts'] }),
		]
	})
}

export default [
	buildPage("Log"),
	buildPage("Nightly"),
	buildPage("TabStats"),
];

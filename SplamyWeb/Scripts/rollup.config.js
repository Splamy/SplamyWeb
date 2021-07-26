import resolve from '@rollup/plugin-node-resolve';
import { defineConfig } from 'rollup';
import svelte from 'rollup-plugin-svelte';
import typescript from 'rollup-plugin-typescript';
import commonjs from '@rollup/plugin-commonjs';
import preprocess from 'svelte-preprocess';
import scss from "rollup-plugin-scss";
import copy from "rollup-plugin-copy";
import postcss from 'rollup-plugin-postcss'
import CleanCss from "clean-css";
import fs from 'fs';

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

function buildCss() {
	return defineConfig({
		input: "css/roll_css.js",
		output: {
			file: `../wwwroot/css/_css.js`,
			format: "esm",
		},
		plugins: [
			scss({
				//output: "../wwwroot/css/style.css",
				output: function (styles, styleNodes) {
					fs.writeFileSync(`../wwwroot/css/style.css`, styles)
					const compressed = new CleanCss().minify(styles).styles;
					fs.writeFileSync(`../wwwroot/css/style.min.css`, compressed)
				},
				failOnError: true,
			}),
			postcss({
				extract: true,
				minimize: true,
			}),
			copy({
				targets: [
					{ src: "node_modules/@mdi/font/fonts/*", dest: '../wwwroot/fonts' },
					{ src: "node_modules/@fontsource/fira-mono/files/*", dest: '../wwwroot/css/files' },
				]
			})
		]
	})
}

export default [
	buildCss(),
	buildPage("Log"),
	buildPage("Nightly"),
	buildPage("TabStats"),
];

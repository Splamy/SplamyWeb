import Prism from 'prismjs';
import "prismjs/themes/prism-tomorrow.css";
import "prismjs/components/prism-csharp.js";
import "prismjs/components/prism-cil.js";
import "prismjs/plugins/line-numbers/prism-line-numbers.js";
import "prismjs/plugins/line-numbers/prism-line-numbers.css";

Prism.languages.mmix = {
	'comment': /%.*/,
	'keyword': {
		'pattern': /\s+[a-zA-Z]+/,
	},
	'component': {
		pattern: /\$(\d{1,3})/,
		alias: 'variable'
	},
	'punctuation': /[,]/
};


export default Prism;


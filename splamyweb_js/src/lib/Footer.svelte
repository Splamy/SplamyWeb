<script lang="ts">
	import Icon from './Icon.svelte';
	import { mdiEmail, mdiGithub, mdiMail, mdiTwitter, mdiYoutube } from '@mdi/js';
	import { onMount } from 'svelte';

	let protectedMail = '<protected>';
	function descrambleMail() {
		protectedMail = ['mail.com', 'g', 'n@', 'splamy'].reverse().join('');
	}
</script>

<footer class="footer">
	<div class="container">
		<div class="columns">
			<div class="column is-4 is-size-7 has-text-left">
				<div>Powered by <strong>ASP.NET Core 6</strong></div>
				<div>
					Presented with <a href="https://kit.svelte.dev/" rel="external" target="_blank"
						><strong>Svelte Kit</strong></a
					>
				</div>
			</div>
			<div class="column is-4 has-text-centered">
				<a rel="external" href="https://twitter.com/Splamyn" target="_blank">
					<Icon path={mdiTwitter} />
				</a>
				<a rel="external" href="https://github.com/Splamy" target="_blank">
					<Icon path={mdiGithub} />
				</a>
				<a
					rel="external"
					href="https://www.youtube.com/channel/UCKscpXWxjhPi91rpugutd7Q"
					target="_blank"
				>
					<Icon path={mdiYoutube} />
				</a>
				<a rel="external" href="mailto:{protectedMail}" on:mouseenter|once={descrambleMail}>
					<Icon path={mdiEmail} />
				</a>
			</div>
			<div class="column is-4 has-text-right">
				<a href="/impress">Impress &amp; Privacy</a>
			</div>
		</div>
	</div>
</footer>

<style lang="scss">
	@import './css/_prelude';
	@import 'bulma/sass/layout/footer';
	@import 'bulma/sass/grid/columns';
	@import 'bulma/sass/elements/container';
</style>

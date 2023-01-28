<script lang="ts">
	import { page } from '$app/stores';

	let title = `${$page.status}: ${$page.error.message}`;
</script>

<h1>{title}</h1>

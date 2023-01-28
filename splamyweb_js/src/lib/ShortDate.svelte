<script lang="ts">
	export let date: string;
	$: datem = typeof date === 'string' ? new Date(date) : date;
</script>

<span title={datem.toLocaleString()}>{datem.toDateString()}</span>

<script lang="ts">
	export let date: string | Date;
	$: datem = typeof date === 'string' ? new Date(date) : date;
</script>

<span title={datem.toLocaleString()}>{datem.toDateString()}</span>

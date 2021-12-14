<script lang="ts">
	import type { Moment } from 'moment';

	// TODO: Allow string or moment
	export let date: Moment;
</script>

<span title={date.toLocaleString()}>{date.toDate().toDateString()}</span>

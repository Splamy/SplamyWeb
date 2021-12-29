<script lang="ts">
	import moment from 'moment';

	export let date: string | moment.Moment;
	$: datem = typeof date === 'string' ? moment(date) : date;
</script>

<span title={datem.toLocaleString()}>{datem.toDate().toDateString()}</span>

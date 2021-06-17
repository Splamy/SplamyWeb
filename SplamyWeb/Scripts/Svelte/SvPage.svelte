<script lang="ts">
	import * as signalR from "@microsoft/signalr";
	import VirtualList from "svelte-tiny-virtual-list";

	const connection = new signalR.HubConnectionBuilder()
		.withUrl("/livelog")
		.build();

	let events: Record<number, LogEvent> = {};
	let list = [];
	let maxId = null;
	let minId = null;
	$: itemCount = maxId - minId;

	connection.on("Log", function (ev: LogEvent) {
		addEvent(ev);
		//updateList();
	});

	connection.on("LogRange", function (evs: LogEvent[]) {
		addRange(evs);
		//updateList();
	});

	init();

	async function init() {
		await connection.start();
		await getTop();
		//updateList();
	}

	async function getTop() {
		addRange(await connection.invoke<LogEvent[]>("GetTop"));
	}

	async function request(from: number, count: number = 50) {
		addRange(await connection.invoke<LogEvent[]>("GetLog", from, count));
	}

	function addRange(evs: LogEvent[]) {
		for (const ev of evs) {
			addEvent(ev);
		}
	}

	function addEvent(ev: LogEvent) {
		maxId = maxId === null ? ev.sequenceID : Math.max(ev.sequenceID, maxId);
		minId = minId === null ? ev.sequenceID : Math.min(ev.sequenceID, minId);
		events[ev.sequenceID] = ev;
	}

	// function updateList() {
	// 	if (follow) {
	// 		const newList: LogEvent[] = [];
	// 		for (let id = 0; id <= 50; id++) {
	// 			const ev = events[maxId - id];
	// 			if (ev) {
	// 				newList.push(ev);
	// 				if (newList.length >= 20) break;
	// 			}
	// 		}
	// 		list = newList;
	// 	}
	// }

	interface LogEvent {
		sequenceID: number;
		formattedMessage: string;
		loggerName: string;
	}
</script>

<div>
	<VirtualList height={500} width="auto" {itemCount} itemSize={50}>
		<div slot="item" let:index let:style {style} class="row">
			{events[maxId - index]?.loggerName}:
			{events[maxId - index]?.formattedMessage}
		</div>
	</VirtualList>
</div>

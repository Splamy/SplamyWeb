<script context="module" lang="ts">
	export const ssr = false;
	export const prerender = false;
</script>

<script lang="ts">
	import Zoomie from './Zoomie.svelte';
	import Collectable from './Collectable.svelte';
	import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
	import { onMount } from 'svelte';
	import { CollectableData, Coord, Particle, Rocket } from './minigame';
	import { BASE_URL, debounced } from '$lib/util';
	import { mdiStar } from '@mdi/js';
	import Icon from '$lib/Icon.svelte';

	const rocketIds = new Map<string, Rocket>();
	let rockets: Rocket[] = [];
	const collectableIds = new Map<number, CollectableData>();
	let collectables: CollectableData[] = [];
	let particlePool: Particle[] = [];
	let particlePoolStart = 0;
	let particlePoolEnd = 0;
	let particleTicker = 0;
	let lastTimeStamp = 0;
	let hasRAF = false;
	const mouse: Coord = { x: 0, y: 0 };
	let isOnline = false;
	let connection: HubConnection | null = null;

	for (let i = 0; i < 100; i++) {
		particlePool.push(new Particle());
	}

	export function add() {
		if (isOnline) return;
		let rocket: Rocket = Rocket.createLocal();
		rocket.color = Rocket.randomColor();
		rocket.position = { x: window.innerWidth / 2, y: window.innerHeight / 2 };
		rocket.angle = Rocket.randomRotation();
		rocket.angleVel = Rocket.randomAngleVel();
		rocket.followTarget = Math.random() > 0.5;

		addRocket(rocket);
	}

	async function connectOnline() {
		clearAllRockets();
		await onlineInit();
	}

	export function addRocket(rocket: Rocket) {
		if (rocketIds.size == 0) {
			document.addEventListener('mousemove', trackMouse);
			lastTimeStamp = performance.now();
		}

		if (!rocketIds.has(rocket.id)) {
			rocketIds.set(rocket.id, rocket);
			rockets = [...rockets, rocket];
		}

		if (!hasRAF) {
			hasRAF = true;
			requestAnimationFrame(animateAll);
		}
	}

	export function removeRocketById(trackId: string) {
		if (rocketIds.delete(trackId)) {
			rockets = rockets.filter((r) => r.id != trackId);
		}
	}

	export function clearAllRockets() {
		rocketIds.clear();
		rockets = [];
	}

	function addCollectable(collectable: CollectableData) {
		collectableIds.set(collectable.id, collectable);
		collectables = [...collectables, collectable];
	}

	function addParticle(position: Coord, angle: number) {
		const index = Math.floor(Math.random() * particlePool.length);
		const particle = particlePool[index];
		particle.position.x = position.x;
		particle.position.y = position.y;
		particle.angle = angle;
		particle.lifetime = 200;
	}

	function animateAll(time: DOMHighResTimeStamp) {
		hasRAF = false;
		if (rocketIds.size == 0) {
			document.removeEventListener('mousemove', trackMouse);
			return;
		}

		const elapsed = time - lastTimeStamp;
		lastTimeStamp = time;

		let spawnParticle = false;
		particleTicker += elapsed;
		if (particleTicker > 50) {
			particleTicker -= 50;
			spawnParticle = true;
		}

		for (const rocket of rocketIds.values()) {
			if (rocket.followTarget) {
				rocket.animateFollow(elapsed);
			} else {
				rocket.animateAuto(elapsed);
			}

			if (spawnParticle)
				addParticle(
					rocket.position,
					(rocket.angle + Math.PI + (Math.random() - 0.5) / 5) % (Math.PI * 2)
				);
		}

		animateParticles(elapsed);

		hasRAF = true;
		requestAnimationFrame(animateAll);
	}

	function animateParticles(elapsed: number) {
		for (const particle of particlePool) {
			particle.animate(elapsed);
		}
	}

	const sendMouse = debounced(
		() => {
			if (connection != null && connection.state == HubConnectionState.Connected) {
				connection.send('SetTarget', mouse);
			}
		},
		1000 / 64,
		{
			callInitial: false,
			resetOnCall: false
		}
	);

	function trackMouse(e: MouseEvent) {
		mouse.x = e.clientX;
		mouse.y = e.clientY;

		if (isOnline) {
			sendMouse();
		} else {
			for (const rocket of rockets) {
				if (rocket.followTarget) {
					rocket.target = mouse;
				}
			}
		}
	}

	type RocketUpdate = { id: string; position: Coord; target: Coord; angle: number };
	type RocketUpdateFull = RocketUpdate & { name: string; color: number; points: number };
	type CookieUpdate = { id: number; position: Coord; active: boolean };
	type InitState = {
		players: RocketUpdateFull[];
		collectibles: CookieUpdate[];
	};

	export async function onlineInit() {
		isOnline = true;
		if (connection != null) return connection.connectionId;

		connection = new HubConnectionBuilder().withUrl(`${BASE_URL}/minigame`).build();

		connection.on('InitState', function (init: InitState) {
			//console.log('InitState', JSON.stringify(init));
			for (const initRocket of init.players) {
				const rocket = Rocket.createOnline(initRocket.id);
				Object.assign(rocket, initRocket);
				addRocket(rocket);
			}
			for (const initColl of init.collectibles) {
				const collectable = new CollectableData();
				Object.assign(collectable, initColl);
				addCollectable(collectable);
			}
		});

		connection.on('PlayerLeft', function (id: string) {
			removeRocketById(id);
		});

		function OnPlayersUpdate(updates: (RocketUpdate | RocketUpdateFull)[]) {
			//console.log('PlayersUpdate', JSON.stringify(updates));
			for (const update of updates) {
				let rocket = rocketIds.get(update.id);
				if (rocket == null) {
					rocket = Rocket.createOnline(update.id);
					addRocket(rocket);
				}
				const diff = diffC(rocket.position, update.position);
				if (Math.abs(diff.x) + Math.abs(diff.y) >= 10) {
					rocket.position = {
						x: (rocket.position.x + update.position.x) / 2,
						y: (rocket.position.y + update.position.y) / 2
					};
				}
				if ('points' in update) {
					rocket.points = update.points;
					rocket.name = update.name;
					rocket.color = update.color;
					rocket.update.update((c) => c + 1);
				}
				rocket.target = update.target;
				rocket.angle = update.angle;
			}
		}
		connection.on('PlayersUpdate', OnPlayersUpdate);
		connection.on('PlayersUpdateState', OnPlayersUpdate);

		connection.on('CookiesUpdate', function (updates: CookieUpdate[]) {
			//console.log('CookiesUpdate', JSON.stringify(updates));
			for (const update of updates) {
				if (update.active) {
					let collectable = collectableIds.get(update.id);
					if (collectable == null) {
						collectable = new CollectableData();
						Object.assign(collectable, update);
						addCollectable(collectable);
					}
				} else {
					collectableIds.delete(update.id);
					collectables = collectables.filter((c) => c.id != update.id);
				}
			}
		});

		await connection.start();
	}

	function diffC(a: Coord, b: Coord) {
		return { x: a.x - b.x, y: a.y - b.y };
	}

	onMount(() => {
		return async () => {
			if (connection != null) {
				await connection.stop();
				connection = null;
			}
		};
	});
</script>

{#if !isOnline}
	<div class="field is-horizontal">
		<button class="button" on:click={add}>Spawn another Rocket</button>
	</div>

	<div class="field is-horizontal">
		<!-- <input
			class="input"
			style="max-width: 10em;"
			type="text"
			bind:value={username}
			placeholder="Username"
		/> -->
		<button class="button" on:click={connectOnline}>Join Online</button>
	</div>
{/if}

{#each particlePool as particle}
	<div style="position:fixed;top:0;left:0;pointer-events: none;">
		<div bind:this={particle.elem} style="width:0; height:0;">
			<Icon path={mdiStar} style={'transform: translate(-12px,-12px) scale(0.6);'} />
		</div>
	</div>
{/each}

{#each rockets as zoomie (zoomie.id)}
	<Zoomie rocket={zoomie} />
{/each}

{#each collectables as coll (coll.id)}
	<Collectable {coll} />
{/each}

<style lang="scss">
	@import '../../lib/css/_prelude';
	@import 'bulma/sass/elements/button';
	@import 'bulma/sass/form/shared';
	@import 'bulma/sass/form/tools';
</style>

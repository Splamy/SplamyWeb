<script lang="ts">
	import Icon from '$lib/Icon.svelte';
	import { BASE_URL } from '$lib/util';
	import { mdiContentSave, mdiDelete, mdiPlus, mdiRefresh, mdiQrcode, mdiClose } from '@mdi/js';
	import type { KeyValue } from '$lib/api';
	import type { PageData } from './$types';
	import { type WgPeer, type EditWgPeer, arrayToB64, b64ToArray, fromEditPeer } from './defs';
	import { generateKeyPair } from './wg';
	import swal from 'sweetalert';
	import QRious from 'qrious';

	export let data: PageData;

	let pkMap = new Map<string, string>();
	let editPeers = toEditPeerList(data.peers);

	let createPeer = emptyWgPeer();
	let privateKey = '';
	let privateKeyValid = true;

	function emptyWgPeer() {
		return {
			publicKey: '',
			friendlyName: '',
			allowedIPs: ''
		};
	}

	function createPeerIsValid() {
		try {
			const arr = b64ToArray(createPeer.publicKey);
			return arr.length == 32;
		} catch (e) {
			return false;
		}
	}

	$: {
		if (privateKey != '') {
			try {
				const pk = b64ToArray(privateKey);
				const res = generateKeyPair(pk);
				createPeer.publicKey = arrayToB64(res.public);
				privateKey = arrayToB64(res.private);
				privateKeyValid = true;
			} catch (e) {
				createPeer.publicKey = '<invalid private key>';
				privateKeyValid = false;
			}
		} else {
			createPeer.publicKey = '';
			privateKeyValid = true;
		}
	}

	function toEditPeerList(peers: WgPeer[]) {
		return peers.map(toEditPeer);
	}

	function toEditPeer(peer: WgPeer): EditWgPeer {
		return {
			publicKey: peer.publicKey,
			privateKey: pkMap.get(peer.publicKey),
			friendlyName: peer.friendlyName,
			allowedIPs: peer.allowedIPs.join(', ')
		};
	}

	async function putPeer(peer: EditWgPeer) {
		const peerDef = fromEditPeer(peer);
		if (peerDef.publicKey == '') {
			return;
		}

		try {
			let response = await fetch(`${BASE_URL}/api/wireguard/peers`, {
				credentials: 'include',
				method: 'PUT',
				headers: {
					Accept: 'application/json',
					'Content-Type': 'application/json'
				},
				body: JSON.stringify(peerDef)
			});
			if (!response.ok) {
				let text = await response.text();
				throw text;
			}

			await refresh();
			return true;
		} catch (e) {
			swal({
				title: 'Failed to save peer',
				text: e.toString(),
				icon: 'error'
			});
			return false;
		}
	}

	async function putCreatePeer() {
		if (!createPeerIsValid()) {
			swal('Invalid public key');
			return;
		}
		if (privateKeyValid && privateKey != '') {
			pkMap.set(createPeer.publicKey, privateKey);
		}
		if (await putPeer(createPeer)) {
			clearCreatePeer();
		}
	}

	async function deletePeer(peer: EditWgPeer) {
		const answer = await swal(`Delete '${peer.friendlyName ?? peer.publicKey}' ?`, {
			dangerMode: true,
			buttons: ['Cancel', 'Delete']
		});
		if (answer !== true) {
			return;
		}

		await fetch(`${BASE_URL}/api/wireguard/peers`, {
			credentials: 'include',
			method: 'DELETE',
			headers: {
				Accept: 'application/json',
				'Content-Type': 'application/json'
			},
			body: JSON.stringify(peer.publicKey)
		});
		await refresh();
	}

	async function reloadService() {
		try {
			await fetch(`${BASE_URL}/api/wireguard/reload`, {
				credentials: 'include',
				method: 'POST'
			});
		} catch (e) {
			swal({
				title: 'Failed to reload service',
				text: e.toString(),
				icon: 'error'
			});
		}
	}

	function randomPrivateKey() {
		const pk = new Uint8Array(32);
		crypto.getRandomValues(pk);
		privateKey = arrayToB64(pk);

		if (createPeer.allowedIPs.trim() == '') {
			const topIp = Math.max(
				...editPeers.map((p) => {
					const m = /\d+\.\d+\.\d+\.(\d+)/.exec(p.allowedIPs);
					return m ? parseInt(m[1]) : 0;
				})
			);
			const nextIp = topIp + 1;
			createPeer.allowedIPs = `10.0.0.${nextIp}/32, fc::${nextIp}/128`;
		}
	}

	async function refresh() {
		const res = await fetch(`${BASE_URL}/api/wireguard/peers`, {
			credentials: 'include'
		});
		editPeers = toEditPeerList((await res.json()) as WgPeer[]);
	}

	async function fetchTemplate() {
		const res = await fetch(`${BASE_URL}/api/store/value/wg_template`, {
			credentials: 'include'
		});
		return await res.text();
	}

	async function showQRCode(peer: EditWgPeer) {
		const size = 512;

		if (peer.privateKey == '') {
			swal('No private key specified');
			return;
		}

		if (peer.allowedIPs.trim() == '') {
			swal('No IPs specified');
			return;
		}

		const template = await fetchTemplate();

		let rendered_template = template.replaceAll(/{(\w+)}/gi, function (x) {
			const key = x.substring(1, x.length - 1);
			switch (key) {
				case 'privateKey':
					return peer.privateKey;
				case 'publicKey':
					return peer.publicKey;
				case 'allowedIPs':
					return peer.allowedIPs;
				case 'friendlyName':
					return peer.friendlyName;
				default:
					return x;
			}
		});

		console.log('Rendered Template', rendered_template);

		var canvas = document.createElement('canvas');
		canvas.width = size;
		canvas.height = size;
		var qr = new QRious({
			element: canvas,
			size,
			value: rendered_template
		});
		const imgData = canvas.toDataURL('image/png');
		const img = document.createElement('img');
		img.src = imgData;

		swal('Tunnel config', {
			content: img
		});
	}

	function showCreateQRCode() {
		if (!privateKeyValid) {
			swal('Invalid private key');
			return;
		}

		if (privateKey == '') {
			swal('No private key specified');
			return;
		}

		createPeer.privateKey = privateKey;
		showQRCode(createPeer);
	}

	function clearCreatePeer() {
		createPeer = emptyWgPeer();
		privateKey = '';
	}
</script>

<svelte:head>
	<title>Wireguard</title>
</svelte:head>

<table class="table" style="width: 100%">
	<thead>
		<tr>
			<th>Peers</th>
			<th />
		</tr>
	</thead>
	<tbody>
		{#each editPeers as peer}
			<tr>
				<td class="compact">
					<input type="text" class="input" bind:value={peer.publicKey} readonly />
					<input type="text" class="input" bind:value={peer.friendlyName} />
					<input type="text" class="input" value={peer.allowedIPs} />
					{#if peer.privateKey != null}
						<input type="text" class="input" value={peer.privateKey} readonly />
					{/if}
				</td>
				<td class="compact">
					<div class="buttons" style="flex-direction: column; align-items: start;">
						<button class="button" on:click={() => putPeer(peer)}>
							<Icon path={mdiContentSave} />
						</button>
						<button class="button" on:click={() => deletePeer(peer)}>
							<Icon path={mdiDelete} />
						</button>
						{#if peer.privateKey != null}
							<button class="button" on:click={() => showQRCode(peer)}>
								<Icon path={mdiQrcode} />
							</button>
						{/if}
					</div>
				</td>
			</tr>
		{/each}

		<tr>
			<td class="compact">
				<input
					type="text"
					class="input"
					placeholder="Public key"
					bind:value={createPeer.publicKey}
					readonly={privateKey != ''}
				/>
				<input
					type="text"
					class="input"
					placeholder="Friendly Name"
					bind:value={createPeer.friendlyName}
				/>
				<input
					type="text"
					class="input"
					placeholder="IPs"
					bind:value={createPeer.allowedIPs}
				/>

				<div class="field has-addons">
					<div class="control is-expanded">
						<input
							type="text"
							class="input"
							placeholder="Private key"
							class:invalid={!privateKeyValid}
							bind:value={privateKey}
						/>
					</div>
					<div class="control">
						<button class="button" on:click={() => randomPrivateKey()}>
							<Icon path={mdiRefresh} />
						</button>
					</div>
				</div>
			</td>
			<td class="compact">
				<div class="buttons" style="flex-direction: column; align-items: start;">
					<button class="button" on:click={() => putCreatePeer()}>
						<Icon path={mdiPlus} />
					</button>
					<button class="button" on:click={() => clearCreatePeer()}>
						<Icon path={mdiClose} />
					</button>
					{#if privateKeyValid && privateKey != ''}
						<button class="button" on:click={() => showCreateQRCode()}>
							<Icon path={mdiQrcode} />
						</button>
					{/if}
				</div>
			</td>
		</tr>
	</tbody>
</table>

<button class="button" on:click={() => reloadService()}>
	<span>Reload Service </span>
	<Icon path={mdiRefresh} />
</button>

<style lang="scss">
	@import '../../../lib/css/_prelude';
	@import 'bulma/sass/elements/table';
	@import 'bulma/sass/form/shared';
	@import 'bulma/sass/form/tools';
	@import 'bulma/sass/form/input-textarea';
	@import 'bulma/sass/elements/button';

	.invalid {
		border-color: $danger !important;
		color: $danger;
	}
</style>

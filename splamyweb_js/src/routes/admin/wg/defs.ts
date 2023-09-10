export interface WgPeer {
	publicKey: string;
	friendlyName?: string;
	allowedIPs: string[];
}

export interface EditWgPeer {
	publicKey: string;
	privateKey?: string;
	friendlyName?: string;
	allowedIPs: string;
}


export function b64ToArray(str: string) {
	return Uint8Array.from(atob(str), (c) => c.charCodeAt(0));
}

export function arrayToB64(arr: Uint8Array) {
	return btoa(String.fromCharCode(...arr));
}

export function toEditPeerList(peers: WgPeer[]) {
	return peers.map(toEditPeer);
}

export function toEditPeer(peer: WgPeer): EditWgPeer {
	return {
		publicKey: peer.publicKey,
		friendlyName: peer.friendlyName,
		allowedIPs: peer.allowedIPs.join(', ')
	};
}

export function fromEditPeer(peer: EditWgPeer): WgPeer {
	return {
		publicKey: peer.publicKey,
		friendlyName: peer.friendlyName,
		allowedIPs: peer.allowedIPs.split(',').map((s) => s.trim())
	};
}

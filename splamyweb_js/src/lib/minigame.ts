import { HubConnection, HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr";
import { get, writable } from "svelte/store";
import { BASE_URL, debounced } from "./util";

export const ZOOMIES = writable<Rocket[]>([]);

export class Rocket {
	public name: string;
	public color: number = Rocket.randomColor();
	public elem: HTMLElement | null = null;
	public position = { x: 0, y: 0 };
	public target = { x: 0, y: 0 };
	public angle = 0.0;
	public angleVel = 0.0;
	public followTarget = false;
	public trackId: string;

	public static createLocal(): Rocket {
		const rocket = new Rocket();
		rocket.trackId = `local_${dummyTrackId}`;
		dummyTrackId++;
		return rocket;
	}

	public static createOnline(id: string): Rocket {
		const rocket = new Rocket();
		rocket.trackId = id;
		rocket.followTarget = true;
		return rocket;
	}

	private static mathMod(n: number, m: number) {
		return ((n % m) + m) % m;
	}

	public static randomRotation() {
		return Math.random() * Math.PI * 2;
	}

	public static randomAngleVel() {
		return (Math.random() - 0.5) / 300;
	}

	public static randomColor() {
		return Math.round(Math.random() * 360);
	}

	public animateAuto(elapsed: number) {
		this.angle += this.angleVel * elapsed;
		let x = this.position.x + Math.cos(this.angle) * (elapsed * SPEED);
		let y = this.position.y + Math.sin(this.angle) * (elapsed * SPEED);

		if (x < 0 || y < 0 || x > window.innerWidth || y > window.innerHeight) {
			this.angleVel = Rocket.randomAngleVel();
			x = Rocket.mathMod(x, window.innerWidth);
			y = Rocket.mathMod(y, window.innerHeight);
		}

		this.position.x = x;
		this.position.y = y;

		if (this.elem != null) {
			this.elem.style.transform = `translate(${x}px, ${y}px) rotate(${this.angle + Math.PI / 2}rad)`;
		}
	}

	public animateFollow(elapsed: number) {
		let x = this.position.x;
		let y = this.position.y;
		const diffX = x - this.target.x;
		const diffY = y - this.target.y;
		const targetAngle = Math.atan2(diffY, diffX);

		const angleDiff = targetAngle - this.angle;
		const angleAdjust = Rocket.mathMod(angleDiff, Math.PI * 2) - Math.PI;
		const angleClamp = Math.min(TURN_SPEED, Math.max(-TURN_SPEED, angleAdjust));

		this.angle += angleClamp * elapsed;

		x += Math.cos(this.angle) * (elapsed * SPEED);
		y += Math.sin(this.angle) * (elapsed * SPEED);

		this.position.x = x;
		this.position.y = y;

		if (this.elem != null) {
			this.elem.style.transform = `translate(${x}px, ${y}px) rotate(${this.angle + Math.PI / 2}rad)`;
		}
	}
}

type Coord = { x: number, y: number };

const rockets = new Map<string, Rocket>();
const TURN_SPEED = 1 / 600;
const SPEED = 1 / 3;
let lastTimeStamp = 0;
let hasRAF = false;
const mouse: Coord = { x: 0, y: 0 };
let dummyTrackId = 0;
let connection: HubConnection | null = null;

export function addRocket(rocket: Rocket) {
	if (rockets.size == 0) {
		document.addEventListener("mousemove", trackMouse);
		lastTimeStamp = performance.now();
	}

	rockets.set(rocket.trackId, rocket);

	if (!hasRAF) {
		hasRAF = true;
		requestAnimationFrame(animateAll);
	}
}

export function removeRocket(rocket: Rocket) {
	rockets.delete(rocket.trackId);
}

function animateAll(time: DOMHighResTimeStamp) {
	hasRAF = false;
	if (rockets.size == 0) {
		document.removeEventListener("mousemove", trackMouse);
		return;
	}

	const elapsed = time - lastTimeStamp;
	lastTimeStamp = time;

	for (const rocket of rockets.values()) {
		if (rocket.followTarget) {
			rocket.animateFollow(elapsed);
		} else {
			rocket.animateAuto(elapsed);
		}
	}

	hasRAF = true;
	requestAnimationFrame(animateAll);
}

const sendMouse = debounced(() => {
	if (connection != null
		&& connection.state == HubConnectionState.Connected) {
		connection.send("SetTarget", mouse);
	}
}, 1000 / 64, {
	callInitial: false,
	resetOnCall: false
});

function trackMouse(e: MouseEvent) {
	mouse.x = e.clientX;
	mouse.y = e.clientY;

	sendMouse();
}

export async function onlineInit() {
	if (connection != null)
		return connection.connectionId;

	connection = new HubConnectionBuilder().withUrl(`${BASE_URL}/minigame`).build();

	connection.on('PlayerJoined', function (id: string) {
		console.log('new player joined', id);
	});

	connection.on('PlayerLeft', function (id: string) {
		const rocket = rockets.get(id);
		rockets.delete(id);
		removeRocket(rocket);
	});

	connection.on('PlayersUpdate', function (updates: { id: string, position: Coord, target: Coord, angle: number }[]) {
		for (const update of updates) {
			let rocket = rockets.get(update.id);
			if (rocket == null) {
				rocket = Rocket.createOnline(update.id);
				addRocket(rocket);
				ZOOMIES.update(z => [...z, rocket]);
			}
			const diff = diffC(rocket.position, update.position);
			if (Math.abs(diff.x) + Math.abs(diff.y) >= 10) {
				rocket.position = { x: (rocket.position.x + update.position.x) / 2, y: (rocket.position.y + update.position.y) / 2 };
			}
			rocket.target = update.target;
			rocket.angle = update.angle;
		}
	});

	await connection.start();

	const self = Rocket.createOnline(connection.connectionId);
	ZOOMIES.update(z => [...z, self]);
}

function diffC(a: Coord, b: Coord) {
	return { x: a.x - b.x, y: a.y - b.y };
}

export function isOnline() { return connection != null; }

export function clearAllRockets() {
	rockets.clear();
	ZOOMIES.update(() => []);
}

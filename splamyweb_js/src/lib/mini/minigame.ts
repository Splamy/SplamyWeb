import { writable } from "svelte/store";

export type Coord = { x: number; y: number };

const TURN_SPEED = 1 / 600;
const SPEED = 1 / 3;

export class Rocket {
	public elem: HTMLElement | null = null;
	private _elemCache: HTMLElement | null = null;
	private _transformElemCache: HTMLElement | null = null;
	private _rotateElemCache: HTMLElement | null = null;
	public id: string;
	public name: string;
	public color: number = Rocket.randomColor();
	public position: Coord = { x: 0, y: 0 };
	public target: Coord = { x: 0, y: 0 };
	public angle = 0.0;
	public angleVel = 0.0;
	public followTarget = false;
	public points = 0;
	public update = writable(0);

	private static dummyTrackId = 0;

	public static createLocal(): Rocket {
		const rocket = new Rocket();
		rocket.id = `local_${Rocket.dummyTrackId}`;
		Rocket.dummyTrackId++;
		return rocket;
	}

	public static createOnline(id: string): Rocket {
		const rocket = new Rocket();
		rocket.id = id;
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

		this.setTransform(x, y);
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

		this.setTransform(x, y);
	}

	private setTransform(x: number, y: number) {
		this.position.x = x;
		this.position.y = y;
		if (this.elem != null) {
			let translateElem = this.elem;
			let rotateElem = this.elem.querySelector<HTMLElement>(".rocket");
			if (this._elemCache !== this.elem) {
				translateElem = this.elem;
				rotateElem = this.elem.querySelector<HTMLElement>(".rocket");
				this._transformElemCache = translateElem;
				this._rotateElemCache = rotateElem;
				this._elemCache = this.elem;
			} else {
				translateElem = this._transformElemCache
				rotateElem = this._rotateElemCache;
			}

			translateElem.style.transform = `translate(${x}px, ${y}px)`;
			rotateElem.style.transform = `rotate(${this.angle + Math.PI / 2}rad)`;
		}
	}
}

export class CollectableData {
	id: number;
	position: Coord;
}


export class Particle {
	public elem?: HTMLElement | null = null;
	public position: Coord = { x: 0, y: 0 };
	public angle: number;
	public lifetime = 0;

	public animate(elapsed: number) {
		if (this.lifetime <= 0) return;
		this.position.x += Math.cos(this.angle) * (elapsed * SPEED);
		this.position.y += Math.sin(this.angle) * (elapsed * SPEED);
		this.lifetime -= elapsed;
		if (this.elem != null) {
			if (this.lifetime > 0) {
				this.elem.style.transform = `translate(${this.position.x}px, ${this.position.y}px)`;
				this.elem.style.color = `rgb(${Math.min(this.lifetime, 255)}, 0, 0)`;
				this.elem.style.opacity = `${Math.min(this.lifetime / 200, 1)}`;
			} else {
				this.elem.style.transform = `translate(-1000px, -1000px)`;
			}
		}
	}
}

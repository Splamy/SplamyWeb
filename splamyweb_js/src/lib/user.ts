import { building } from "$app/environment";
import { writable } from "svelte/store";
import type { LoginResult } from "./api";
import { BASE_URL } from "./util";

export class User {
	constructor(
		public readonly id: number,
		public readonly name: string,
		public readonly rank: number,
	) { }

	public static readonly DUMMY: User = new User(0, "", 0);
}

export const CurrentUser = writable<User | null>(null);

export function applyLoginResult(result: LoginResult | null) {
	if (result?.loggedIn) {
		CurrentUser.set(new User(result.user.id, result.user.name, result.user.rank));
		return;
	}
	CurrentUser.set(null);
}

export async function fetchCurrentUser() {
	if (building) return;
	try {
		const res = await fetch(`${BASE_URL}/account/whoami`, {
			credentials: "include",
		});
		const json = await res.json() as LoginResult;
		applyLoginResult(json);
		return;
	} catch (e) { console.warn("Couldn't get login info"); }
	CurrentUser.set(null);
}

fetchCurrentUser();

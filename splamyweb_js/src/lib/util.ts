const BUILD_MODE = import.meta.env.MODE;
const IS_PRODUCTION = BUILD_MODE === 'production';
export const BASE_URL = IS_PRODUCTION ? "" : 'http://localhost:44422';
type FuncTyp<T extends unknown[]> = (...args: T) => void;
interface DebounceOpt {
	/**
	 * When true, resets timer on each new call. Does not fire until the timer ran out.<br>
	 * **Default**: false
	 */
	resetOnCall?: boolean;

	/**
	 * When true, calls the function once when starting the timer.<br>
	 * **Default**: false
	 */
	callInitial?: boolean;
}

declare const window: Window;

export function debounced<T extends unknown[] = []>(
	fn: FuncTyp<T>,
	timeout: number,
	options?: DebounceOpt
) {
	let timer: number | undefined;
	let lastArgs: T;
	const resetOnCall = options?.resetOnCall ?? false;
	const callInitial = options?.callInitial ?? false;

	function cancel() {
		if (timer !== undefined) {
			clearTimeout(timer);
			timer = undefined;
		}
	}

	function call(...args: T) {
		lastArgs = args;
		if (resetOnCall) {
			cancel();
		}

		if (timer === undefined) {
			timer = window.setTimeout(() => {
				timer = undefined;
				fn(...lastArgs);
			}, timeout);
			if (callInitial) fn(...args);
		}
	}

	function flush() {
		if (timer !== undefined) {
			cancel();
			fn(...lastArgs);
		}
	}

	call.cancel = cancel;
	call.call = call;
	call.flush = flush;
	return call;
}

export async function sleep(timeout: number): Promise<void> {
	return new Promise((resolve) => setTimeout(resolve, timeout));
}

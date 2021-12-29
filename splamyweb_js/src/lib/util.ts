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

export function debounced<T extends unknown[] = []>(
	fn: FuncTyp<T>,
	timeout: number,
	options?: DebounceOpt
) {
	let timer: ReturnType<typeof setTimeout> | undefined;
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
			timer = setTimeout(() => {
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

// this action (https://svelte.dev/tutorial/actions) allows us to
// progressively enhance a <form> that already works without JS
export function enhance(
	form: HTMLFormElement,
	{
		pending,
		error,
		result
	}: {
		pending?: (data: FormData, form: HTMLFormElement) => void;
		error?: (res: Response, error: Error, form: HTMLFormElement) => void;
		result: (res: Response, form: HTMLFormElement) => void;
	}
): { destroy: () => void } {
	let current_token: unknown;

	async function handle_submit(e: Event) {
		const token = (current_token = {});

		e.preventDefault();

		const body = new FormData(form);

		if (pending) pending(body, form);

		try {
			const res = await fetch(form.action, {
				method: form.method,
				credentials: 'include',
				headers: {
					accept: 'application/json'
				},
				body
			});

			if (token !== current_token) return;

			if (res.ok) {
				result(res, form);
			} else if (error) {
				error(res, null, form);
			} else {
				console.error(await res.text());
			}
		} catch (e) {
			if (error) {
				error(null, e, form);
			} else {
				throw e;
			}
		}
	}

	form.addEventListener('submit', handle_submit);

	return {
		destroy() {
			form.removeEventListener('submit', handle_submit);
		}
	};
}

export function autosize(el: HTMLTextAreaElement) {
	const setHeight = () => {
		el.style.height = '5px';
		el.style.height = `${el.scrollHeight}px`;
	};

	setHeight();
	el.addEventListener('input', setHeight);
}

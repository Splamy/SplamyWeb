type SwalIconType = "info" | "success" | "warning" | "error";
interface SwalOptions {
	readonly title?: string;
	readonly text?: string;
	readonly icon?: SwalIconType;
	readonly button?: SwalButton;
	readonly buttons?: boolean | SwalButton[] | { [button: string]: SwalButton; };
	readonly dangerMode?: boolean;
	readonly content?: "input";
	readonly showCancelButton?: boolean;
	readonly showConfirmButton?: boolean;
}
interface SwalButtonOptions {
	readonly text?: string;
	readonly value?: string;
	readonly closeModal?: boolean;
}
type SwalButton = boolean | string | SwalButtonOptions;
type SwalReturn = null | string;

declare function swal(options: SwalOptions): Promise<SwalReturn>;
declare function swal(text: string, options?: SwalOptions): Promise<SwalReturn>;
declare function swal(title: string, text: string): Promise<SwalReturn>;
declare function swal(title: string, text: string, icon: SwalIconType, options?: SwalOptions): Promise<SwalReturn>;
declare module swal {
	function stopLoading(): void;
	function close(): void;
	function setActionValue(text: string): void;
}

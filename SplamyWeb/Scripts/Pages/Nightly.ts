import 'sweetalert';
import { SweetAlert } from 'sweetalert/typings/core';
declare const swal: SweetAlert;

export async function askDeleteNightly(project: string, branch: string) {
	const answer = await swal(`Delete ${project}/${branch} ?`, {
		dangerMode: true,
		buttons: ["Cancel", "Delete"],
	});
	if (answer === true) {
		try {
			const response = await fetch(`api/nightly/${project}/${branch}`, {
				method: "DELETE",
				credentials: "include",
				redirect: "follow",
			});
			if (!response.ok) {
				throw response;
			}

			swal.close();
		} catch {
			await swal("Failed to delete branch", {
				icon: "error"
			});
		}
	}
}

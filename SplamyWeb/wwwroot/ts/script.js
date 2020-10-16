async function ask_delete_nightly(project, branch) {
    const answer = await swal(`Delete ${project}/${branch} ?`, {
        dangerMode: true,
        button: {
            text: "Delete",
            closeModal: false,
        },
    });
    if (answer) {
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
        }
        catch (_a) {
            await swal("Failed to delete branch", {
                icon: "error"
            });
        }
    }
}
//# sourceMappingURL=script.js.map
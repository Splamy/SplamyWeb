var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
function ask_delete_nightly(project, branch) {
    return __awaiter(this, void 0, void 0, function* () {
        let answer = yield swal(`Delete ${project}/${branch} ?`, {
            dangerMode: true,
            button: {
                text: "Delete",
                closeModal: false,
            },
        });
        if (answer) {
            try {
                var response = yield fetch(`api/nightly/${project}/${branch}`, {
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
                yield swal("Failed to delete branch", {
                    icon: "error"
                });
            }
        }
    });
}
//# sourceMappingURL=script.js.map
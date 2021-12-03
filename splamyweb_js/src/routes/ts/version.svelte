<script lang="ts">
	function checkFn(event) {
		checkResult();
		event.preventDefault();
		return false;
	}

	// TODO sveltify !!!

	async function checkResult() {
		const version = encodeURIComponent(document.querySelector("input[name='version']").value);
		const platform = encodeURIComponent(document.querySelector("input[name='platform']").value);
		const sign = encodeURIComponent(document.querySelector("input[name='sign']").value);
		var resp = await fetch(`/api/teamspeak/version/${version}/${platform}?sign=${sign}`, {
			method: 'POST'
		});
		var result = await resp.json();

		document.getElementById('result_msg').classList.remove('is-hidden');
		const res_txt = document.getElementById('result_txt');
		res_txt.classList.remove('is-info', 'is-danger');
		if (typeof result === 'string') {
			res_txt.classList.add('is-info');
			res_txt.innerText = result;
		} else if ('error' in result) {
			res_txt.classList.add('is-danger');
			res_txt.innerText = result.error;
		} else {
			res_txt.classList.add('is-danger');
			res_txt.innerText = 'The api seems down, try again later...';
		}
	}
</script>

<svelte:head>
	<title>TS Version</title>
</svelte:head>

<h1 class="title">Teamspeak Version Checker</h1>

<article class="section readblock">
	<div class="tile is-ancestor is-vertical">
		<div class="tile is-parent">
			<form id="check_form" class="tile is-child box" on:submit={checkFn}>
				<div class="field">
					<label class="label" for="fld_version">Version</label>
					<div class="control">
						<input
							id="fld_version"
							name="version"
							class="input"
							placeholder="e.g. '3.0.11 [Build: 1374563791]'"
						/>
					</div>
				</div>
				<div class="field">
					<label class="label" for="fld_platform">Platform</label>
					<div class="control">
						<input
							id="fld_platform"
							name="platform"
							class="input"
							placeholder="e.g. 'Windows'"
						/>
					</div>
				</div>
				<div class="field">
					<label class="label" for="fld_sign">Sign</label>
					<div class="control">
						<input
							id="fld_sign"
							name="sign"
							class="input"
							placeholder="e.g. 'hQCwiLP5f4GIcDG5KQ1T+CNFGqRxyw5MXCHE8KjWRIgkjCuGSryK4vpPy70EURH3blQ8TKrax8BEorHlpnpdAQ=='"
						/>
					</div>
				</div>

				<div class="field">
					<div class="control">
						<button type="submit" class="button is-primary" on:click={checkFn}
							>Validate</button
						>
					</div>
				</div>

				<div id="result_msg" class="field is-hidden">
					<div id="result_txt" class=" notification is-info" />
				</div>
			</form>
		</div>

		<div class="tile is-parent">
			<article class="tile is-child notification is-primary">
				You can check out all collected versions
				<a
					rel="external"
					href="https://github.com/ReSpeak/tsdeclarations/blob/master/Versions.csv"
					target="_blank"
				>
					here<span class="icon"><i class="mdi mdi-open-in-new mdi-18px" /></span>
				</a>
			</article>
		</div>
	</div>
</article>

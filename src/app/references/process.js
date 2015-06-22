function process(sourceId, targetId, indexName) {
	return new Promise(function (resolve, reject) {
		// Start writing your code here.
		// Call resolve() to signal that the processing has finished.
		// Call reject() to signal that something went wrong.
		console.log(sourceId, targetId, indexName);
		setTimeout(function () {
			resolve("This is DONE!!");
		}, 3000);
	});
}

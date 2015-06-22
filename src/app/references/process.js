function process(source, target) {
	return new Promise(function(resolve, reject) {
		// Start writing your code here.
		// Call resolve() to signal that the processing has finished.
		// Call reject() to signal that something went wrong.
		console.log(source);
		console.log(target);
		resolve("This is DONE!!");
	});
}

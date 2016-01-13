'use strict';

var gulp = require('gulp');
var browserSync = require('browser-sync');

function isOnlyChange(event) {
  return event.type === 'changed';
}

module.exports = function(options) {
  gulp.task('watch', ['inject', 'swagger'], function () {

    gulp.watch(['{' + options.src + ',' + options.common + '}' + '/*.html', 'bower.json'], ['inject']);

    gulp.watch([
      '{' + options.src + ',' + options.common + '}' + '/**/*.css',
      '{' + options.src + ',' + options.common + '}' + '/**/*.scss'
    ], function(event) {
      if(isOnlyChange(event)) {
        gulp.start('styles');
      } else {
        gulp.start('inject');
      }
    });

    gulp.watch([
      '{' + options.src + ',' + options.common + '}' + '/**/*.js',
      '{' + options.src + ',' + options.common + '}' + '/**/*.ts'
    ], function(event) {
      if(isOnlyChange(event)) {
        gulp.start('scripts');
      } else {
        gulp.start('inject');
      }
    });

    gulp.watch('{' + options.src + ',' + options.common + '}' + '/**/*.html', function(event) {
      browserSync.reload(event.path);
    });
  });
};

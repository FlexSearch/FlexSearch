'use strict';

var gulp = require('gulp');
var browserSync = require('browser-sync');
var mkdirp = require('mkdirp');

var $ = require('gulp-load-plugins')();

module.exports = function(options) {
  gulp.task('scripts', ['tsd:install'], function () {
    mkdirp.sync(options.tmp);

    return gulp.src(options.src + '/app/**/*.ts')
      .pipe($.sourcemaps.init())
      .pipe($.tslint())
      .pipe($.tslint.report('prose', { emitError: false }))
      .pipe($.typescript({sortOutput: true})).on('error', options.errorHandler('TypeScript'))
      .pipe($.sourcemaps.write())
      .pipe($.toJson({filename: options.tmp + '/sortOutput.json', relative:true}))
      .pipe(gulp.dest(options.tmp + '/serve/app'))
      .pipe(browserSync.reload({ stream: true }))
      .pipe($.size());
  });
};

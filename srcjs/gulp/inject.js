'use strict';

var gulp = require('gulp');

var $ = require('gulp-load-plugins')();

var wiredep = require('wiredep').stream;

module.exports = function(options) {
  gulp.task('inject', ['scripts', 'styles'], function () {
    var injectStyles = gulp.src([
      options.tmp + '/serve/app/**/*.css',
      '!' + options.tmp + '/serve/app/vendor.css'
    ], { read: false });

    var sortOutput = require('../' + options.tmp + '/sortOutput.json');

    var injectScripts = gulp.src([
      '{' + options.src + ',' + options.common + ',' + options.tmp + '/serve}/app/**/*.js',
      '!' + options.src + '/**/*.spec.js',
      '!' + options.src + '/**/*.mock.js',
      '!' + options.src + '/references/process.js'
    ], { read: false })
    .pipe($.order(sortOutput, {base: options.tmp + '/serve/app'}));

    var injectOptions = {
      ignorePath: [options.src, options.common, options.tmp + '/serve'],
      addRootSlash: false
    };

    return gulp.src(options.common + '/index.html')
      .pipe($.inject(injectStyles, injectOptions))
      .pipe($.inject(injectScripts, injectOptions))
      .pipe(wiredep(options.wiredep))
      .pipe(gulp.dest(options.tmp + '/serve'));

  });
};

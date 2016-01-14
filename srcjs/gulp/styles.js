'use strict';

var gulp = require('gulp');
var browserSync = require('browser-sync');
var merge = require('merge-stream');
var $ = require('gulp-load-plugins')();

var wiredep = require('wiredep').stream;

module.exports = function (options) {
    gulp.task('styles-' + options.name, function () {
        var sassOptions = {
            style: 'expanded'
        };

        var injectFiles = gulp.src([
            '{' + options.src + ',' + options.common + '}' + '/**/*.scss',
            '!' + '{' + options.src + ',' + options.common + '}' + '/index.scss',
            '!' + '{' + options.src + ',' + options.common + '}' + '/vendor.scss'
        ], { read: false });

        var injectOptions = {
            transform: function (filePath) {
                filePath = filePath.replace(options.src, '');
                return '@import \'' + filePath + '\';';
            },
            starttag: '// injector',
            endtag: '// endinjector',
            addRootSlash: false
        };

        var indexFilter = $.filter('index.scss');
        var vendorFilter = $.filter('vendor.scss');

        var scss = gulp.src([
            options.common + '/index.scss',
            options.src + '/vendor.scss'
        ])
            .pipe(indexFilter)
            .pipe($.inject(injectFiles, injectOptions))
            .pipe(indexFilter.restore())
            .pipe(vendorFilter)
            .pipe(wiredep(options.wiredep))
            .pipe(vendorFilter.restore())
            .pipe($.sourcemaps.init())
            .pipe($.sass(sassOptions)).on('error', options.errorHandler('Sass'))
            .pipe($.autoprefixer()).on('error', options.errorHandler('Autoprefixer'))
            .pipe($.sourcemaps.write())
            .pipe(gulp.dest(options.tmp + '/serve/app/'))
            .pipe(browserSync.reload({ stream: true }));

        var css = gulp.src(options.src + '/**/*.css')
            .pipe(gulp.dest(options.tmp + '/serve/app/' + options.name + '/'));

        return merge(scss, css);
    });
};

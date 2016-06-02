'use strict';

var gulp = require('gulp');

var $ = require('gulp-load-plugins')({
    pattern: ['gulp-*', 'main-bower-files', 'uglify-save-license', 'del']
});

var swaggerDir = __dirname + '\\..\\..\\spec\\';

module.exports = function (options) {
    gulp.task('partials-' + options.name, function () {

        return gulp.src([
            options.src + '/**/*.html',
            options.common + '/**/*.html',
            options.tmp + '/serve/app/**/*.html'
        ])
            .pipe($.minifyHtml({
                empty: true,
                spare: true,
                quotes: true
            }))
            .pipe($.angularTemplatecache('templateCacheHtml.js', {
                module: 'flexportal'/*,
                root: 'app'*/
            }))
            .pipe(gulp.dest(options.tmp + '/partials/'));
    });

    gulp.task('html-' + options.name, ['inject-' + options.name, 'partials-' + options.name], function () {
        var partialsInjectFile = gulp.src(options.tmp + '/partials/templateCacheHtml.js', { read: false });
        var partialsInjectOptions = {
            starttag: '<!-- inject:partials -->',
            ignorePath: options.tmp + '/partials',
            addRootSlash: false
        };

        var htmlFilter = $.filter('*.html');
        var jsFilter = $.filter('**/*.js');
        var cssFilter = $.filter('**/*.css');
        var assets;

        return gulp.src(options.tmp + '/serve/*.html')
            .pipe($.inject(partialsInjectFile, partialsInjectOptions))
            .pipe(assets = $.useref.assets({ searchPath: [options.tmp + '/serve', options.tmp + '/partials', options.src, options.common] }))
            .pipe($.rev())
            .pipe(jsFilter)
            .pipe($.ngAnnotate())
            .pipe($.uglify({ preserveComments: $.uglifySaveLicense })).on('error', options.errorHandler('Uglify'))
            .pipe(jsFilter.restore())
            .pipe(cssFilter)
            .pipe($.csso())
            .pipe(cssFilter.restore())
            .pipe(assets.restore())
            .pipe($.useref())
            .pipe($.revReplace())
            .pipe(htmlFilter)
            .pipe($.minifyHtml({
                empty: true,
                spare: true,
                quotes: true,
                conditionals: true
            }))
            .pipe(htmlFilter.restore())
            .pipe(gulp.dest(options.dist + '/'))
            .pipe($.size({ title: options.dist + '/', showFiles: true }));
    });

    // Only applies for fonts from bower dependencies
    // Custom fonts are handled by the "other" task
    gulp.task('fonts-' + options.name, function () {
        return gulp.src($.mainBowerFiles())
            .pipe($.filter('**/*.{eot,svg,ttf,woff,woff2}'))
            .pipe($.flatten())
            .pipe(gulp.dest(options.dist + '/fonts/'));
    });

    gulp.task('other-' + options.name, function () {
        return gulp.src([
            options.src + '/**/*',
            options.common + '/**/*',
            '!' + options.src + '/**/*.{html,css,js,scss,ts}',
            '!' + options.common + '/**/*.{html,css,js,scss,ts}'
        ])
            .pipe(gulp.dest(options.dist + '/'));
    });

    // Task for copying over js libraries that should remain untouched
    gulp.task('pure-libs-' + options.name, function () {
        return gulp.src(options.common + '/references/*')
            .pipe($.filter('process.js'))
            .pipe(gulp.dest(options.tmp + '/serve/scripts'))
            .pipe(gulp.dest(options.dist + '/scripts'))
    });

    gulp.task('clean-' + options.name, function (done) {
        $.del([options.dist + '/', options.tmp + '/'], done);
    });

    gulp.task('swagger-' + options.name, function () {
        // Swagger file is only needed for the swagger module
        if (options.name == 'swagger') {
            return gulp.src(swaggerDir + "swagger-full.json")
                .pipe($.rename('swagger.json'))
                .pipe(gulp.dest(options.src))
                .pipe(gulp.dest(options.tmp))
                .pipe(gulp.dest(options.dist))
        }
    });

    gulp.task('build-' + options.name, ['html-' + options.name, 'fonts-' + options.name, 'other-' + options.name, 'swagger-' + options.name]);
};

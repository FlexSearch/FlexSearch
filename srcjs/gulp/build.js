'use strict';

var gulp = require('gulp');
var fs = require('fs');
var $ = require('gulp-load-plugins')({
  pattern: ['gulp-*', 'main-bower-files', 'uglify-save-license', 'del']
});

var convert = require('swagger-converter');
var swaggerDir = __dirname + '\\..\\..\\documentation\\';
var swagger1_2 = require(swaggerDir + 'swagger.json');

module.exports = function(options) {
  gulp.task('partials', function () {
    return gulp.src([
      options.src + '/app/**/*.html',
      options.tmp + '/serve/app/**/*.html'
    ])
      .pipe($.minifyHtml({
        empty: true,
        spare: true,
        quotes: true
      }))
      .pipe($.angularTemplatecache('templateCacheHtml.js', {
        module: 'flexportal',
        root: 'app'
      }))
      .pipe(gulp.dest(options.tmp + '/partials/'));
  });

  gulp.task('html', ['inject', 'partials'], function () {
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
      .pipe(assets = $.useref.assets())
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
  gulp.task('fonts', function () {
    return gulp.src($.mainBowerFiles())
      .pipe($.filter('**/*.{eot,svg,ttf,woff,woff2}'))
      .pipe($.flatten())
      .pipe(gulp.dest(options.dist + '/fonts/'));
  });
  
  gulp.task('other', function () {
    return gulp.src([
      options.src + '/**/*',
      '!' + options.src + '/**/*.{html,js,css,ts}'
    ])
      .pipe(gulp.dest(options.dist + '/'));
  });

  // Task for copying over js libraries that should remain untouched
  gulp.task('pure-libs', function() {
    return gulp.src(options.src + '/app/references/*')
      .pipe($.filter('process.js'))
      .pipe(gulp.dest(options.tmp + '/serve/scripts'))
      .pipe(gulp.dest(options.dist + '/scripts'))
  })

  gulp.task('clean', ['tsd:purge'], function (done) {
    $.del([options.dist + '/', options.tmp + '/'], done);
  });

  gulp.task('swagger', function() {
    $.util.log("Building swagger...");
    var swagger2_0 = convert(swagger1_2, []);
    swagger2_0.info.title = "FlexSearch API"
    var swagger2Uri = swaggerDir + "swagger_v2.json"; 
    fs.writeFile(swagger2Uri, JSON.stringify(swagger2_0));
    
    return gulp.src(swagger2Uri)
      .pipe(gulp.dest(options.src))
      .pipe(gulp.dest(options.tmp))
      .pipe(gulp.dest(options.dist))
  });

  gulp.task('build', ['html', 'fonts', 'other', 'pure-libs', 'swagger']);
};

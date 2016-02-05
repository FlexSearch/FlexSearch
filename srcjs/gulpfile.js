'use strict';

var gulp = require('gulp');
var gutil = require('gulp-util');
var wrench = require('wrench');
var fs = require('fs');
var path = require('path');
var gulps = [];
var runSequence = require('run-sequence');

function getFolders(dir) {
    return fs.readdirSync(dir)
        .filter(function (file) {
            return fs.statSync(path.join(dir, file)).isDirectory();
        });
}

wrench.readdirSyncRecursive('./gulp')
    .filter(function (file) {
        return (/\.(js|coffee)$/i).test(file);
    }).map(function (file) {
        gulps.push(require('./gulp/' + file));
    });

// Build the SPAs for each app
var taskNames = getFolders('src/apps').map(function (appFolder) {
    var relFolderPath = 'src/apps/' + appFolder;
    
    var options = {
        // This name is used so that we create unique gulp task names for each app
        name: appFolder,
        src: relFolderPath,
        common : 'src/common', 
        dist: relFolderPath + '/dist',
        tmp: relFolderPath + '/.tmp',
        e2e: relFolderPath + '/e2e',
        errorHandler: function (title) {
            return function (err) {
                gutil.log(gutil.colors.red('[' + title + ']'), err.toString());
                this.emit('end');
            };
        },
        wiredep: {
            directory: 'bower_components'
        }
    };

    gutil.log("gulping " + appFolder);
    gulps.map(function(g) { g(options); });
    
    gulp.task(appFolder, ['clean-' + appFolder], function () {
        gutil.log("Building app " + appFolder + "...");
        return gulp.start('build-' + appFolder);
    });
    
    return appFolder;
});

gulp.task('default', function() {
   gutil.log("Building all apps...");
   return runSequence.apply(null, taskNames);
});

'use strict';

var gulp = require('gulp');
var gutil = require('gulp-util');
var wrench = require('wrench');
var fs = require('fs');
var path = require('path');

var options = {
    src: 'src',
    dist: 'dist',
    tmp: '.tmp',
    e2e: 'e2e',
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

function getFolders(dir) {
    return fs.readdirSync(dir)
        .filter(function (file) {
            return fs.statSync(path.join(dir, file)).isDirectory();
        });
}

// Build the SPAs for each app
getFolders('src/apps').map(function (appFolder) {
    var relFolderPath = 'src/apps/' + appFolder;
    gutil.log("Building app " + appFolder + "...");
    
    var options = {
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

    wrench.readdirSyncRecursive('./gulp').filter(function (file) {
        return (/\.(js|coffee)$/i).test(file);
    }).map(function (file) {
        require('./gulp/' + file)(options);
    });

    gulp.task('default', ['clean'], function () {
        gulp.start('build');
    });

});

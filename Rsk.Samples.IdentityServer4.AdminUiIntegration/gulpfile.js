/*
	Gulp tasks for: IdentityServer
	Author: @karltynan for Rock Solid Knowledge Ltd (https://www.rocksolidknowledge.com/)
*/

var gulp = require('gulp');
var purgecss = require('gulp-purgecss');
var rename = require('gulp-rename');
var uglify = require('gulp-uglify');
var uglifycss = require('gulp-uglifycss');

// purgecss path
var purgecssPath = [
	'./views/**/*.cshtml'
];

// asset paths
var cssSourcePath = './src/css/site.css';
var printSourcePath = './src/css/print.css';
var jsSourcePath = './src/js/*.js';

// export paths
var cssExportPath = './wwwroot/css/';
var jsExportPath = './wwwroot/js/';

// custom purgecss extractor(s)
// https://github.com/FullHuman/purgecss
class TailwindExtractor {
	static extract(content) {
		return content.match(/[A-z0-9-:\/]+/g);
	}
}

// site css
function css() {
	return gulp.src(cssSourcePath)
		.pipe(gulp.dest(cssExportPath))
		.pipe(purgecss({
			content: purgecssPath,
			extractors: [
				{
					extractor: TailwindExtractor,
					extensions: ['html', 'cshtml']
				}
			]
		}))
		.pipe(uglifycss({
			"maxLineLen": 312,
			"uglyComments": true
		}))
		.pipe(rename({ suffix: '.min' }))
		.pipe(gulp.dest(cssExportPath));
}

// print css
function print() {
	return gulp.src(printSourcePath)
		.pipe(gulp.dest(cssExportPath))
		.pipe(uglifycss({
			"maxLineLen": 312,
			"uglyComments": true
		}))
		.pipe(rename({ suffix: '.min' }))
		.pipe(gulp.dest(cssExportPath));
}

function js() {
	return gulp.src(jsSourcePath)
		.pipe(gulp.dest(jsExportPath))
		.pipe(uglify())
		.pipe(rename({ suffix: '.min' }))
		.pipe(gulp.dest(jsExportPath));
}

// watch
function watchFiles() {
	gulp.watch(['./src/css/site.css'], css);
	gulp.watch(['./src/css/print.css'], print);
	gulp.watch(['./src/js/**/*.js'], js);
}

const build = gulp.parallel(css, print, js);
const watcher = gulp.series(build, watchFiles);

// exports
exports.build = build;
exports.css = gulp.parallel(css, print);
exports.js = js;
exports.watch = watcher;
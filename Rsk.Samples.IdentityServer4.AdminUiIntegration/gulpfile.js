/// <binding ProjectOpened='build' />
/*
	Gulp tasks for: IdentityServer
	Author: @karltynan for Rock Solid Knowledge Ltd (https://www.rocksolidknowledge.com/)
*/

var gulp = require('gulp');
var cssimport = require("gulp-cssimport");
var gulpIf = require('gulp-if');
var postcss = require('gulp-postcss');
var purgecss = require('gulp-purgecss');
var rename = require('gulp-rename');
var tailwindcss = require('tailwindcss');
var uglify = require('gulp-uglify');
var uglifycss = require('gulp-uglifycss');

var usePurgecss = true;

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
var printExportPath = './wwwroot/css/';
var jsExportPath = './wwwroot/js/';

// custom purgecss extractor(s) https://github.com/FullHuman/purgecss
class TailwindExtractor {
	static extract(content) {
		return content.match(/[A-z0-9-:\/]+/g);
	}
}

// site css
function css() {
	return gulp.src(cssSourcePath)
		.pipe(cssimport())
		.pipe(postcss([
			tailwindcss('./tailwind.js'),
			require('postcss-nested'),
			require('autoprefixer')
		]))
		.pipe(gulp.dest(cssExportPath))
		.pipe(
			gulpIf(usePurgecss, purgecss({
				content: purgecssPath,
				extractors: [
					{
						extractor: TailwindExtractor,
						extensions: ['html', 'cshtml']
					}
				]
			}))
		)
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
		.pipe(gulp.dest(printExportPath));
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
	gulp.watch(['./Views/**/*.cshtml'], buildCss);
	gulp.watch(['./src/css/**/*.css'], buildCss);
	gulp.watch(['./src/js/**/*.js'], js);
}

const buildCss = gulp.parallel(css, print);
const build = gulp.parallel(buildCss, js);
const watcher = gulp.series(build, watchFiles);

// exports
exports.build = build;
exports.css = buildCss;
exports.js = js;
exports.watch = watcher;
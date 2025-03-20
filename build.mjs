import fs from "fs";
import archiver from "archiver"
import path from "path"
import config from "./build.json" assert { type: "json" }
import {globSync} from "glob"

console.log(`Building version=${config.info.version}, spt=${config.info.spt}`)

if (fs.existsSync("tmp/")) {
	fs.rmSync("tmp/", {recursive: true})
}

if (!fs.existsSync("dist/")) {
	fs.mkdirSync("dist/")
}

console.log("Creating tmp directory")
fs.mkdirSync("tmp/")

console.log("Creating directory tree")
for (const dest in config.dest) {
	console.log(`\t ${config.dest[dest]}`)
	fs.mkdirSync("tmp/" + config.dest[dest], {recursive: true})
}

console.log("Copying all files")
for (const [src, dest] of Object.entries(config.files)) {
	if (fs.lstatSync(src).isDirectory()) {
		const dstDir = "tmp/" + config.dest[dest] + path.basename(src) + "/"
		fs.mkdirSync(dstDir, {recursive: true})

		globSync("**", {ignore: config.ignore, cwd: "./" + src}).forEach(f => {
			const fSrc = src + "/" + f
			const dst = dstDir + f
			if(fs.lstatSync(fSrc).isDirectory()) {
				fs.mkdirSync(dst, {recursive: true})
			} else {
				console.log(`\t Copy ${fSrc} -> ${dst}`)
				fs.cpSync(fSrc, dst, {recursive: true})
			}
		})
	} else {
		const dst = "tmp/" + config.dest[dest] + path.basename(src)
		console.log(`\t Copy ${src} -> ${dst}`)
		fs.cpSync(src, dst, {recursive: true})
	}

}

console.log("Creating zip")
await zipDirectory("tmp/", `dist/${config.info.name}-${config.info.version}-spt-${config.info.spt}.zip`)

console.log("Done")
fs.rmSync("tmp/", {recursive: true})

function zipDirectory(sourceDir, outPath) {
	const archive = archiver('zip', { zlib: { level: 9 }})
	const stream = fs.createWriteStream(outPath)
  
	return new Promise((resolve, reject) => {
	  archive
		.directory(sourceDir, false)
		.on('error', err => reject(err))
		.pipe(stream)
  
	  stream.on('close', () => resolve())
	  archive.finalize()
	});
  }
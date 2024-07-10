import fs from "fs";
import path, { parse } from "path";
import { S3Client, PutObjectCommand } from "@aws-sdk/client-s3";
import { gzipSync } from "zlib";
import mime from "mime";
import PQueue from "p-queue";
import { argv } from "process";
import * as semver from "semver";
import {config} from "dotenv";

config({
	debug: true,
	path: ['.env.local', '.env'],
});

const basePath = process.env.BASE_PATH;;
const version = argv[2] || "1.0.0";
const s3Endpoint = process.env.R2_ENDPOINT;
const s3Bucket = process.env.R2_BUCKET_NAME;
const s3AccessKey = process.env.R2_ACCESS_KEY;
const s3Secret = process.env.R2_SECRET_KEY;

/**
 *
 * @param {string} path
 * @returns {string}
 */
function normalizePathToFileName(path) {
	return path.replace(/\\/g, "/").replace(/\//g, "_");
}

/**
 *
 * @param {string} version
 * @param {string} path
 * @returns {string}
 */
function computeFileName(version, path) {
	const normalizedPath = normalizePathToFileName(path);
	return `${version}${normalizedPath}`;
}

/**
 *
 * @param {string} filePath
 * @returns {Buffer | undefined}
 */
function getFileContent(filePath) {
	try {
		return fs.readFileSync(filePath);
	} catch (error) {
		return undefined;
	}
}

/**
 *
 * @param {S3Client} client
 * @param {string} bucket
 * @param {string} fileName
 * @param {Buffer} fileContent
 */
async function uploadFile(client, bucket, fileName, fileContent) {
	const command = new PutObjectCommand({
		ContentEncoding: "gzip",
		Bucket: bucket,
		Key: fileName,
		ContentType: mime.getType(fileName),
		Body: gzipSync(fileContent),
	});
	console.log(`Uploading file ${fileName}`);
	await client.send(command);
}

/**
 *
 * @param {string} basePath
 * @returns {fs.Dirent[]}
 */
function getAllFiles(basePath) {
	const files = fs.readdirSync(basePath, {
		withFileTypes: true,
		recursive: true,
	});

	return files;
}

function uploadFiles() {
	const parsedVersion = semver.parse(version.split('@')[0]);
	
	if(!parsedVersion) {
		console.error("Invalid version format. Please use semver format (e.g. 1.0.0)");
		return;
	}

	console.log("---------Configuration--------");
	console.log(`Version             : ${parsedVersion.version}`);
	console.log(`Base Path           : ${basePath}`);
	console.log(`S3 Client Endpoint  : ${s3Endpoint}`);
	console.log(`S3 Client Bucket    : ${s3Bucket}`);
	console.log(`S3 Client Access Key: ${s3AccessKey}`);
	console.log(`S3 Client Secret    : ${s3Secret}`);
	console.log("------------------------------");
	
	const folderPath = path.resolve(process.cwd(), basePath);
	const files = getAllFiles(folderPath);
	const client = new S3Client({
		region: "auto",
		endpoint: s3Endpoint,
		credentials: {
			accessKeyId: s3AccessKey,
			secretAccessKey: s3Secret,
		},
	});

	const queue = new PQueue({
		concurrency: 20,
	});

	files.forEach((file) => {
		if (file.isFile()) {
			const fileContent = getFileContent(path.resolve(file.path, file.name));

			if (fileContent) {
				queue.add(() =>
					uploadFile(
						client,
						s3Bucket,
						computeFileName(
							parsedVersion.version,
							path.resolve(file.path.replace(folderPath, ""), file.name)
						),
						fileContent
					).catch((error) => {
						console.error(`Error uploading file ${file.name}: ${error}`);
					})
				);
			} else {
				console.log(`File ${file.name} not found`);
			}
		}
	});
	queue.start();
}

uploadFiles();

// const client = new S3Client({
// 	region: "auto",
// 	endpoint: s3Endpoint,
// 	credentials: {
// 		accessKeyId: s3AccessKey,
// 		secretAccessKey: s3Secret,
// 	},
// });

// uploadFile(
// 	client,
// 	s3Bucket,
// 	computeFileName(
// 		version,
// 		"/7/_framework/blazor.webassembly.js"
// 	),
// 	fs.readFileSync(
// 		path.resolve(process.cwd(), "./dist/7/_framework/blazor.webassembly.js")
// 	)
// );

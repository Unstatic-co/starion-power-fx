import { S3Client, DeleteObjectsCommand, ListObjectsCommand } from "@aws-sdk/client-s3";
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
 * @param {S3Client} client
 * @param {string} basePath
 * @returns {Promise<{Key: string}[]>}
 */
async function getAllFiles(client, basePath) {
	const command = new ListObjectsCommand({
		Bucket: s3Bucket,
		Prefix: basePath,
		MaxKeys: 1000000,
	});
	
	const response = await client.send(command);

	return response.Contents?.map((content) => {
		return {
			Key: content.Key,
		};
	}) ?? [];
}

async function deleteFiles() {
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
	
	const client = new S3Client({
		region: "auto",
		endpoint: s3Endpoint,
		credentials: {
			accessKeyId: s3AccessKey,
			secretAccessKey: s3Secret,
		},
	});
	
	const files = await getAllFiles(client, parsedVersion.version);

	const deleteCommand = new DeleteObjectsCommand({
		Bucket: s3Bucket,
		Delete: {
			Objects: files,
		},
	});

	client.send(deleteCommand);
}

deleteFiles();

import io
import base64
import requests as r
from pathlib import Path
from typing import Dict, List

import aiohttp
from PIL import Image


async def fetch(url, method="get"):
	async with aiohttp.ClientSession() as session:
		async with getattr(session, method)(url) as resp:
			return await resp.json()


async def post(url, json):
	async with aiohttp.ClientSession() as session:
		async with session.post(url, json=json) as resp:
			return await resp.json()


class Controller:
	serverAddress = "https://localhost:{}/api/faceEmbeddings/"
	imsize: int = 200

	def __init__(self, server_port: str):
		self.url = self.serverAddress.format(server_port)
		self.img_url = self.url + "images"
		self.compare_url = self.url + "compare"
		self.cancel_url = self.url + "cancel"

	async def getImages(self) -> Dict:
		images = await fetch(self.img_url)
		
		for img in images:
			img['name'] = Path(img['name']).stem
			img['data'] = Image.open(io.BytesIO(base64.b64decode(
				img['data']
			))).resize((self.imsize, self.imsize))
		return images

	async def deleteImage(self, id: int) -> bool:
		return await fetch(self.img_url + f"/id?id={id}", method="delete")

	async def processImages(self, images: List[str]) -> List[int]:
		return await post(self.img_url, images)

	async def clear(self) -> bool:
		return await fetch(self.img_url, method="delete")

	async def compare(self, id1: int, id2: int) -> Dict:
		return await fetch(self.compare_url + f"?id1={id1}&id2={id2}")

	async def cancel(self) -> bool:
		return await fetch(self.cancel_url)

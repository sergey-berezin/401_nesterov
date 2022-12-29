import base64
import json
import io
import streamlit as st
import asyncio
from PIL import Image
from aiohttp.client_exceptions import ClientConnectorError

from controller import Controller


def make_grid(cols, rows):
    grid = [0] * cols
    for i in range(cols):
        with st.container():
            grid[i] = st.columns(rows)
    return grid


async def main():
    st.markdown("# Расчет попарной схожести лиц")

    con = Controller("7262")
    try:
        cancelled = False
        if st.button("Cancel"):
            cancelled = await con.cancel()


        with st.form("my-form", clear_on_submit=True):
            images = st.file_uploader(
                "Загрузите изображения",
                type=["png", "jpg", "jpeg"],
                accept_multiple_files=True
            )
            submitted = st.form_submit_button("Обработать")

            if submitted and images:
                query_data = [
                    {
                        'name': item.name,
                        'data': base64.b64encode(item.getvalue()).decode()
                    }
                    for i, item in enumerate(images)
                ]

                st.text(f"Изображений послано на обработку: {len(query_data)}")
                ids = await con.processImages(query_data)
                st.text(ids)
                st.text(f"Обработано")

                n = len(ids) + 1
                grid = make_grid(n, n)
                for i in range(1, n):
                    img = Image.open(io.BytesIO(
				        images[i - 1].getvalue()
			        )).resize((100, 100))
                    grid[0][i].image(img)
                    grid[i][0].image(img)

                for i, id1 in enumerate(ids):
                    for j, id2 in enumerate(ids):
                        metrics = await con.compare(id1, id2)
                        metrics_str = []
                        if metrics and not cancelled:
                            for name, value in metrics.items():
                                metrics_str.append(f"{name[:4]}: {value:.2f}")
                        else:
                            metrics_str.append("<Empty>")
                        grid[i + 1][j + 1].text("\n".join(metrics_str))
    except ClientConnectorError as ex:
         st.text(f"Ошибка соединения с сервером: {ex}")

asyncio.run(main())

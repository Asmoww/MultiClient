from bs4 import BeautifulSoup
from selenium import webdriver
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.by import By
from selenium.common.exceptions import TimeoutException
from discord_webhook import DiscordWebhook, DiscordEmbed
from datetime import datetime
from time import sleep, perf_counter
import sched, time, json, threading
from math import ceil
from time import perf_counter
from concurrent.futures import wait, ALL_COMPLETED, ThreadPoolExecutor

webhook_url = "https://discord.com/api/webhooks/971437949596102796/JoLhMIXhPyr3ECApJZYtEIrnRHKZoimOJrujYFXjLfyiZZO3auurrmPACV-jTlmDHzQ1"
links = [
        "https://www.verkkokauppa.com/fi/outlet/yksittaiskappaleet?sort=price%3Aasc&filter=brand%3ADJI",
        #"https://www.verkkokauppa.com/fi/outlet/yksittaiskappaleet"
        ]
#links = ["https://www.verkkokauppa.com/fi/outlet/yksittaiskappaleet?filter=brand%3AABC+Design", "https://www.verkkokauppa.com/fi/outlet/yksittaiskappaleet?filter=brand%3AAEG"]
verkkokauppa_logo = "https://pbs.twimg.com/profile_images/1145562519039283200/pfRACtCr_400x400.png"

cooldown = 300

def log(message):
    print("["+datetime.now().strftime("%H:%M:%S")+"] "+message)

def save_products(new_data, link, filename='products.json'):
    with open(filename,'r+') as file:
        file_data = json.load(file)
        for product in new_data:
            file_data[link].update(product)
        file.seek(0)
        json.dump(file_data, file, indent = 2)

def remove_products(products, link, filename='products.json'):
    with open(filename, "r") as readfile:
         file_data = json.load(readfile)
    with open(filename,'w') as writefile:
        for id in products:
            del file_data[link][str(id)]
        writefile.seek(0)
        json.dump(file_data, writefile, indent = 2)

def add_filter(link, filename='products.json'):
    with open(filename,'r+') as file:
        file_data = json.load(file)
        filterdata = {link:{}}
        file_data.update(filterdata)
        file.seek(0)
        json.dump(file_data, file, indent = 2)

def filter_exists(link, filename='products.json'):
    with open(filename,'r') as file:
        data = json.load(file)
        return link in data

def check_id(data, link, searchid):
    return str(searchid) in data[link]
         
def get_data(data, link, id, value):
    return data[link][str(id)][value]

def change_price(link, id, value, filename="products.json"):
    with open(filename, "r") as readfile:
         file_data = json.load(readfile)
    with open(filename,'w') as writefile:
        for i, outletid in enumerate(id):
            file_data[link][str(outletid)]["discountprice"]=value[i]
        writefile.seek(0)
        json.dump(file_data, writefile, indent = 2)

def get_discount(oldprice, newprice):
    return round((1-(newprice/oldprice))*100, 1)

def pricechange_webhook(name, link, oldprice, newprice, condition, outletid, olddiscount, newdiscount):
    #log("Sending price change to Discord... ID "+str(outletid)+" "+str(oldprice)+"€ >> "+str(newprice)+"€")
    webhook = DiscordWebhook(url=webhook_url)
    if newprice < oldprice:
        pricechange=":chart_with_downwards_trend:"
    else:
        pricechange=":chart_with_upwards_trend:"
    embed = DiscordEmbed(title="Price changed  "+pricechange, color='0062ff')
    embed.description = "["+name+"]("+link+")"
    embed.set_footer(text="verkkokauppa.com/outlet • "+str(outletid)+condition, icon_url=verkkokauppa_logo)
    embed.add_embed_field(name='Price', value="**"+str(oldprice)+"**€ >> **"+str(newprice)+"**€")
    embed.add_embed_field(name='Discount', value="**"+str(olddiscount)+"**% >> **"+str(newdiscount)+"**%")
    embed.set_timestamp()
    webhook.add_embed(embed)
    response = webhook.execute()

def newproduct_webhook(name, link, oldprice, newprice, condition, info, outletid):
    #log("Sending new product listing to Discord... ID "+str(outletid))
    webhook = DiscordWebhook(url=webhook_url)
    embed = DiscordEmbed(title="New product added!", color='1fb800')
    embed.description = "["+name+"]("+link+")"
    embed.set_footer(text="verkkokauppa.com/outlet • "+str(outletid)+condition, icon_url=verkkokauppa_logo)
    embed.add_embed_field(name='Price', value="~~"+str(oldprice)+"€~~  **"+str(newprice)+"**€\n**"+str(get_discount(oldprice, newprice))+"**% off")
    embed.add_embed_field(name='Condition & info', value="**"+condition+"**\n"+info)
    embed.set_timestamp()
    webhook.add_embed(embed)
    response = webhook.execute()

def removeproduct_webhook(name, link, oldprice, newprice, condition, info, outletid):
    #log("Sending product removal to Discord... ID "+str(outletid))
    webhook = DiscordWebhook(url=webhook_url)
    embed = DiscordEmbed(title="Product removed.", color='a80303')
    embed.description = "["+name+"]("+link+")"
    embed.set_footer(text="verkkokauppa.com/outlet • "+str(outletid)+condition, icon_url=verkkokauppa_logo)
    embed.add_embed_field(name='Price', value="~~"+str(oldprice)+"€~~  **"+str(newprice)+"**€\n**"+str(get_discount(oldprice, newprice))+"**% off")
    embed.add_embed_field(name='Condition & info', value="**"+condition+"**\n"+info)
    embed.set_timestamp()
    webhook.add_embed(embed)
    response = webhook.execute()

def load_page(url):
    pageload_start = perf_counter()
    #print(url)
    browser.get(url)
    delay = 30 
    myElem = WebDriverWait(browser, delay).until(EC.presence_of_element_located((By.ID, "sort")))
    soup = BeautifulSoup(browser.page_source, "html.parser")
    pageload_stop = perf_counter()
    log("Loaded page in "+str(round(pageload_stop-pageload_start, 3))+" seconds ")
    #print(soup)
    return soup

options = webdriver.ChromeOptions()
options.add_argument('--headless')
browser = webdriver.Chrome(options=options, executable_path=r"C:\Users\pyrys\Desktop\chromedriver.exe")

def search_products(link):
    totaltime_start = perf_counter()

    if not "?" in link:
        edited_url = link+"?"
    else:
        edited_url = link

    if not filter_exists(link):
        add_filter(link)

    file = open('products.json')
    data = json.load(file)

    log("Getting product count from selected filter options...")
    log(link)

    firstsoup = load_page(edited_url)

    productcount = int(firstsoup.find("span", class_="Badge-sc-ih8odx-0 cTCIkV").text)
    pagecount = ceil(productcount/48)
    log(str(productcount)+" products found on "+str(pagecount)+" pages, checking if any are new...")

    pagelinks = []
    pages = []

    log("Loading "+str(pagecount)+" product pages...")
    pagesload_start = perf_counter()
    for i in range(pagecount):
        pagelinks.append(edited_url+"&pageNo="+str(i+1))

    """pool = ThreadPoolExecutor(max_workers=1)
    results = pool.map(load_page, pagelinks)
    for result in results:
        pages.append(result)"""

    for page in pagelinks:
        pages.append(load_page(page))

    pagesload_stop = perf_counter()
    
    log("Loaded all ("+str(pagecount)+") pages in "+str(round(pagesload_stop-pagesload_start, 3))+" seconds.")

    newitemcount = 0
    pricechanges = 0
    newproductdata = []
    productids = []
    pricechangeids = []
    pricechangevalues = []
    parsetimetotal_start = perf_counter()

    for page in pages:

        products = page.find_all("div", class_="Box-sc-eb7m1u-0 bwUubG sc-1v658w8-2 ZDvWU")

        parsetime_start = perf_counter()
        for product in products:

            outletid = int(product['data-product-id'])
            productids.append(outletid)

            discountprice = float(product.find("data", class_="CurrentData-sc-1eckydb-0 dTQfuU").text.replace(",", ".").replace(" ", ""))
            originalprice = float(product.find("data", class_="PreviousData-sc-1eckydb-1 bmjQcW").text.replace(",", ".").replace(" ", ""))
            discount = get_discount(originalprice, discountprice)
            name = product.find("h1", class_="UI-sc-1m8dr2d-12 Name-sc-1y28yl7-0 itUXTI jSFiYR").text
            condition = product.find("div", class_="eRVRaH").text
            try:
                info = product.find("div", class_="sc-ahgb59-4 duhkZE").text
            except:
                info = "No info"
            pictureurl = product.find("picture", class_="Picture-sc-uof9rq-0 cIuzTB").find("img", recursive=False)['srcset'].split()[0]
            urlsplit = edited_url.split("?", 1)
            try:
                producturl = urlsplit[0]+"/"+str(outletid)+"?"+urlsplit[1]
            except:
                producturl = urlsplit[0]+"/"+str(outletid)

            if check_id(data, link, outletid): 
                if not get_data(data, link, outletid, "discountprice")==discountprice:
                    pricechanges += 1
                    oldprice = get_data(data, link, outletid, "discountprice")
                    olddiscount = get_discount(originalprice, oldprice)
                    #log(str(outletid)+" Price has changed, saving new price...")
                    pricechangeids.append(outletid)
                    pricechangevalues.append(discountprice)
                    pricechange_webhook(name, producturl, oldprice, discountprice, condition, outletid, olddiscount, discount)
            
            else:
                newitemcount += 1
                #log("New item found!\nNAME "+name+"\nID "+str(outletid)+" CONDITION "+condition+"\nPRICE was "+str(originalprice)+"€ is "+str(discountprice)+"€ DISCOUNT "+str(discount)+"%\nINFO "+info+"\n")
                newproduct_webhook(name, producturl, originalprice, discountprice, condition, info, outletid)

                productdata = {
                    outletid:
                        {
                        "name":name,
                        "discountprice": discountprice,
                        "originalprice": originalprice,
                        "condition": condition,
                        "info": info
                        }
                    }

                newproductdata.append(productdata)
        parsetime_stop = perf_counter()
        log("Checked "+str(len(products))+" products in "+str(round(parsetime_stop-parsetime_start, 3))+" seconds.")

    removedproducts = []

    for id in data[link]:
        if not int(id) in productids:
            removedproducts.append(id)
            try:
                producturl = urlsplit[0]+"/"+id+"?"+urlsplit[1]
            except:
                producturl = urlsplit[0]+"/"+id
            removeproduct_webhook(data[link][id]["name"], producturl, data[link][id]["originalprice"], data[link][id]["discountprice"], data[link][id]["condition"], data[link][id]["info"], int(id))
    
    file.close()

    remove_products(removedproducts, link)
    
    parsetimetotal_stop = perf_counter()
    log("Checked all ("+str(productcount)+") products in "+str(round(parsetimetotal_stop-parsetimetotal_start, 3))+" seconds.")
    
    if newitemcount == 0:
        log("No new products were added.")
    else: 
        if newitemcount == 1:
            log("1 new product was added!")
        else:
            log(str(newitemcount)+" new products were added!")

    if pricechanges == 0:
        log("No prices were changed.")
    else: 
        if pricechanges == 1:
            log("1 price was changed.")
        else:
            log(str(pricechanges)+" prices were changed.")

    if len(removedproducts) == 0:
        log("No products were removed.")
    else: 
        if len(removedproducts) == 1:
            log("1 product was removed.")
        else:
            log(str(len(removedproducts))+" products were removed.")

    if newitemcount > 0 or pricechanges > 0:
        log("Saving new info...")
        savetime_start = perf_counter()
        if newitemcount > 0:
            save_products(newproductdata, link)
        if pricechanges > 0:
            change_price(link, pricechangeids, pricechangevalues)
        savetime_stop = perf_counter()
        log("Saved new info in "+str(round(savetime_stop-savetime_start, 3))+" seconds.")

    totaltime_stop = perf_counter()
    log("Finished checking "+str(productcount)+" products on "+str(pagecount)+" pages in "+str(round(totaltime_stop-totaltime_start, 3))+" seconds.\n")

def timer():
    while True:
        log("Cooldown of "+str(cooldown)+" seconds has ended, checking products from "+str(len(links))+" links.\n")
        for link in links:
            search_products(link)
        time.sleep(cooldown)

t = threading.Thread(target=timer)
t.start()





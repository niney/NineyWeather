import urllib.request
from bs4 import BeautifulSoup

def PageCrawler(recipeUrl):
    url = 'http://www.10000recipe.com/' + recipeUrl

    req = urllib.request.Request(url)
    sourcecode = urllib.request.urlopen(url).read()
    soup = BeautifulSoup(sourcecode, "html.parser")

    recipe_title = [] #레시피 제목
    recipe_source = {} #레시피 재료
    recipe_step = [] #레시피 순서
    recipe_tag = [] #레시피 해시태그

    res = soup.find('div','view2_summary')
    res = res.find('h3')
    recipe_title.append(res.get_text())
    res = soup.find('div','view2_summary_info')
    recipe_title.append(res.get_text().replace('\n',''))

    res = soup.find('div','ready_ingre3')

    #재료 찾는 for문 가끔 형식에 맞지 않는 레시피들이 있어 try / except 해준다.
    try :
        for n in res.find_all('ul'):
            source = []
            title = n.find('b').get_text()
            recipe_source[title] = ''
            for tmp in n.find_all('li'):
                source.append(tmp.get_text().replace('\n','').replace(' ',''))
            recipe_source[title] = source
    except (AttributeError):
        return

    #요리 순서 찾는 for문
    res = soup.find('div','view_step')
    i = 0
    for n in res.find_all('div','view_step_cont'):
        i = i + 1
        recipe_step.append('#' + str(i) + ' ' + n.get_text().replace('\n',' '))
        #나중에 순서를 구분해주기 위해 숫자와 #을 넣는다.

    #해시태그가 글 내에 있는지 판단하고 출력해주는 for문
    if (res.find('div','view_tag')):
        recipe_tag = res.find('div','view_tag').get_text()
        #del recipe_tag[0]

    #블로그 형식의 글은 스텝이 정확하게 되어있지 않기 때문에 제외해준다
    if not recipe_step:
        return

    recipe_all = [recipe_title, recipe_source, recipe_step, recipe_tag]
    return(recipe_all)

def PageDetailCrawler(recipeUrl):
    url = 'http://www.10000recipe.com/' + recipeUrl

    req = urllib.request.Request(url)
    sourcecode = urllib.request.urlopen(url).read()
    soup = BeautifulSoup(sourcecode, "html.parser")

    recipe_title = [] #레시피 제목
    recipe_source = {} #레시피 재료
    recipe_step = [] #레시피 순서
    recipe_tag = [] #레시피 해시태그

    res = soup.find('div','rcp_m_list2')
    for n in res.find_all('div', 'col-xs-3'):
        # i = i + 1
        # recipe_step.append('#' + str(i) + ' ' + n.get_text().replace('\n',' '))
        print(n.find('img').attrs['src'])
        print(n.find('h4').get_text())

    # recipe_title.append(res.get_text())
    # res = soup.find('div','view2_summary_info')
    # recipe_title.append(res.get_text().replace('\n',''))

    # res = soup.find('div','ready_ingre3')

    # print(recipe_title)


# print(PageCrawler('recipe/6900135'))
PageDetailCrawler('/recipe/reco_list.html')

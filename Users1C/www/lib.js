function GetLastRecords(doc)
{
	if (window.XMLHttpRequest)
	{// code for IE7+, Firefox, Chrome, Opera, Safari
	   xmlhttp=new XMLHttpRequest();
	}
	else
	{// code for IE6, IE5
	   xmlhttp=new ActiveXObject("Microsoft.XMLHTTP");
	}
	xmlhttp.open("GET","http://127.0.0.1:8080/top.xml",false);
	xmlhttp.send();
	xmlDoc=xmlhttp.responseXML;
	
	var str = "<table><tbody><tr id='myListHeader'><td>Имя пользователя</td><td>Компьютер</td><td>Приложение</td><td>Время начала сеанса</td><td>Номер сеанса</td><td>Номер соединения</td></tr>";
	var x=xmlDoc.getElementsByTagName("Node");
	for (i=0;i<x.length;i++)
	{
	  str = str + "<tr><td>"+
              x[i].getElementsByTagName("UserFullName")[0].childNodes[0].nodeValue+"</td><td>" +
			  x[i].getElementsByTagName("ComputerName")[0].childNodes[0].nodeValue + "</td><td>" +
	  		  x[i].getElementsByTagName("ApplicationName")[0].childNodes[0].nodeValue+"</td><td>"+
			  x[i].getElementsByTagName("ConnectionStarted")[0].childNodes[0].nodeValue+"</td><td>"+
	  		  x[i].getElementsByTagName("SessionNumber")[0].childNodes[0].nodeValue+"</td><td>" +
			  x[i].getElementsByTagName("ConnectionNumber")[0].childNodes[0].nodeValue+"</td></tr>";
	}
	str = str + "</tbody></table>";
	doc.innerHTML = str;
};
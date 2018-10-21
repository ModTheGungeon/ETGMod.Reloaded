return function(env)
	function env.Notify(data)
	  if type(data) ~= "table" then
	    error("Notification data must be a table")
	  end

	  if data.title == nil or data.content == nil then
	    error("Notification must have a title and content")
	  end
	  local notif = gui.Notification(data.title, data.content)

	  if data.image then
	    notif.Image = data.image
	  end
	  if data.background_color then
	    notif.BackgroundColor = data.background_color
	  end
	  if data.title_color then
	    notif.TitleColor = data.title_color
	  end
	  if data.content_color then
	    notif.ContentColor = data.content_color
	  end

	  gui.GUI.NotificationController:Notify(notif)
	end

  function env.Color(r, g, b, a)
    if a == nil then
      return etgmod.UnityUtil.NewColorRGB(r, g, b)
    else
      return etgmod.UnityUtil.NewColorRGBA(r, g, b, a)
    end
  end
end